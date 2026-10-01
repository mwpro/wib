import { test, expect } from '@playwright/test'
import { resetTestUser, cleanupTestChores, cleanupTestRewards, setWalletBalance } from '../helpers/db'

const STORE_USER = {
  sub: 'auth0|test-store-user',
  name: 'Ania',
  email: 'ania@test.com',
}

test.describe.serial('Store & Voucher Wallet UI Tests', () => {
  test.beforeEach(async ({ page }) => {
    await resetTestUser(STORE_USER.sub)
    await cleanupTestChores('STORE_CHORE_')
    await cleanupTestRewards('STORE_REWARD_')
    await page.addInitScript((user) => {
      localStorage.setItem('wib_test_user', JSON.stringify(user))
    }, STORE_USER)
  })

  test.afterAll(async () => {
    await resetTestUser(STORE_USER.sub)
    await cleanupTestChores('STORE_CHORE_')
    await cleanupTestRewards('STORE_REWARD_')
  })

  test('renders store tab sections and empty states', async ({ page }) => {
    await page.goto('/')

    // Navigate to Store tab
    const storeTab = page.getByRole('tab', { name: /Sklep/i })
    await storeTab.click()
    await expect(storeTab).toHaveAttribute('data-state', 'active')

    // Header and spendable balance
    await expect(page.getByRole('heading', { name: 'Sklep z nagrodami' })).toBeVisible()
    await expect(page.getByText(/Twój portfel: 0 pkt/i)).toBeVisible()
    await expect(page.getByRole('button', { name: /Dodaj nagrodę/i })).toBeVisible()

    // Section 1: Mój portfel (expanded by default, shows empty state)
    await expect(page.getByRole('heading', { name: 'Mój portfel' })).toBeVisible()
    await expect(page.getByText(/Twój portfel jest pusty/i)).toBeVisible()

    // Section 2: Dostępne nagrody (expanded by default)
    await expect(page.getByRole('heading', { name: 'Dostępne nagrody' })).toBeVisible()

    // Section 3: Historia zrealizowanych (collapsed by default)
    await expect(page.getByRole('heading', { name: 'Historia zrealizowanych' })).toBeVisible()
  })

  test('creates a custom reward item via modal and displays stock badge', async ({ page }) => {
    await page.goto('/')

    // Navigate to Store tab
    await page.getByRole('tab', { name: /Sklep/i }).click()

    // Open Add Reward Modal
    await page.getByRole('button', { name: /Dodaj nagrodę/i }).click()

    // Fill form
    const rewardTitle = `STORE_REWARD_${Date.now()}`
    await page.getByLabel(/Nazwa nagrody/i).fill(rewardTitle)
    await page.getByLabel(/Koszt w punktach/i).fill('25')
    await page.getByLabel(/Liczba sztuk/i).fill('2')
    await page.getByLabel(/Opis nagrody/i).fill('Relaksujący masaż pleców')

    const createPromise = page.waitForResponse(
      (res) => res.url().includes('/api/store/items') && res.status() === 201
    )
    await page.getByRole('button', { name: 'Dodaj nagrodę' }).click()
    await createPromise

    // Verify card in catalog
    const card = page.locator('[data-testid="reward-card"]', { hasText: rewardTitle })
    await expect(card).toBeVisible()
    await expect(card.getByText('25 pkt')).toBeVisible()
    await expect(card.getByText('Zostało: 2 szt.')).toBeVisible()
    await expect(card.getByText('Relaksujący masaż pleców')).toBeVisible()

    // With 0 balance, purchase button should be disabled
    const buyButton = card.getByRole('button', { name: /Kup nagrodę/i })
    await expect(buyButton).toBeDisabled()
  })

  test('Golden Journey: earn points, purchase reward with confirmation dialog, and redeem voucher', async ({ page }) => {
    // 1. Initial setup: earn 50 points by completing chores
    await page.goto('/')

    const choreTitle = `STORE_CHORE_${Date.now()}`
    const quickAddForm = page.locator('[data-testid="quick-add-chore"]')
    await quickAddForm.getByPlaceholder(/Dodaj nowe zadanie/i).fill(choreTitle)
    await quickAddForm.getByRole('button', { name: 'Dodaj' }).click()

    const choreCard = page.locator('[data-testid="chore-card"]', { hasText: choreTitle })
    await expect(choreCard).toBeVisible()

    // Set wallet balance directly to 50 pkt for fast and predictable test
    // First trigger member provisioning by opening page
    await expect(page.getByText('wib')).toBeVisible()
    await setWalletBalance(STORE_USER.sub, 50)

    // Reload to refresh member wallet balance in header
    await page.reload()
    await expect(page.getByText('50 pkt').first()).toBeVisible()

    // 2. Navigate to Store tab
    await page.getByRole('tab', { name: /Sklep/i }).click()
    await expect(page.getByText(/Twój portfel: 50 pkt/i)).toBeVisible()

    // 3. Create a test reward item
    const rewardTitle = `STORE_REWARD_${Date.now()}`
    await page.getByRole('button', { name: /Dodaj nagrodę/i }).click()
    await page.getByLabel(/Nazwa nagrody/i).fill(rewardTitle)
    await page.getByLabel(/Koszt w punktach/i).fill('20')
    await page.getByLabel(/Liczba sztuk/i).fill('3')
    await page.getByRole('button', { name: 'Dodaj nagrodę' }).click()

    const rewardCard = page.locator('[data-testid="reward-card"]', { hasText: rewardTitle })
    await expect(rewardCard).toBeVisible()

    // 4. Purchase reward -> triggers PurchaseConfirmDialog
    const buyButton = rewardCard.getByRole('button', { name: /Kup nagrodę/i })
    await expect(buyButton).toBeEnabled()
    await buyButton.click()

    // Confirm dialog is displayed with cost details
    const purchaseDialog = page.getByRole('dialog')
    await expect(purchaseDialog.getByRole('heading', { name: /Potwierdź zakup nagrody/i })).toBeVisible()
    await expect(purchaseDialog.getByText(/Czy na pewno chcesz wymienić/i)).toBeVisible()
    await expect(purchaseDialog.getByText('20 pkt')).toBeVisible()

    const purchasePromise = page.waitForResponse(
      (res) => res.url().includes('/purchase') && res.status() === 201
    )
    await purchaseDialog.getByRole('button', { name: /Kupuję nagrodę/i }).click()
    await purchasePromise

    // Dialog closes
    await expect(page.getByRole('dialog')).not.toBeVisible()

    // Wallet balance should now be 30 pkt (50 - 20)
    await expect(page.getByText(/Twój portfel: 30 pkt/i)).toBeVisible()

    // Stock in catalog decremented from 3 to 2
    await expect(rewardCard.getByText('Zostało: 2 szt.')).toBeVisible()

    // 5. Voucher appears in "Mój portfel"
    const voucherCard = page.locator('[data-testid="voucher-card"]', { hasText: rewardTitle })
    await expect(voucherCard).toBeVisible()
    await expect(voucherCard.getByText('20 pkt')).toBeVisible()

    // 6. Redeem voucher -> triggers RedeemConfirmDialog
    const redeemButton = voucherCard.getByRole('button', { name: /Zrealizuj kupon/i })
    await redeemButton.click()

    const redeemDialog = page.getByRole('dialog')
    await expect(redeemDialog.getByRole('heading', { name: /Realizacja kuponu/i })).toBeVisible()
    await expect(redeemDialog.getByText(/Czy na pewno chcesz oznaczyć kupon/i)).toBeVisible()

    const redeemPromise = page.waitForResponse(
      (res) => res.url().includes('/redemption') && res.status() === 200
    )
    await redeemDialog.getByRole('button', { name: 'Zrealizuj kupon' }).click()
    await redeemPromise

    // Dialog closes and voucher is removed from "Mój portfel"
    await expect(page.getByRole('dialog')).not.toBeVisible()
    await expect(voucherCard).not.toBeVisible()
    await expect(page.getByText(/Twój portfel jest pusty/i)).toBeVisible()

    // 7. Expand "Historia zrealizowanych" -> voucher is present
    const historyHeader = page.locator('button', { hasText: 'Historia zrealizowanych' })
    await historyHeader.click()

    const redeemedCard = page.locator('[data-testid="redeemed-voucher-card"]', { hasText: rewardTitle })
    await expect(redeemedCard).toBeVisible()
    await expect(redeemedCard.getByText('20 pkt')).toBeVisible()
  })
})
