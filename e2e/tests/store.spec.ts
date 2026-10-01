import { test, expect } from '@playwright/test'
import {
  resetTestUser,
  cleanupTestChores,
  cleanupTestRewards,
  seedTestUserWithBalance,
} from '../helpers/db'

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

  test('persists active store tab in URL and restores upon refresh or direct navigation', async ({ page }) => {
    // 1. Direct navigation to /store
    await page.goto('/store')

    const storeTab = page.getByRole('tab', { name: /Sklep/i })
    await expect(storeTab).toHaveAttribute('data-state', 'active')
    await expect(page.getByRole('heading', { name: 'Sklep z nagrodami' })).toBeVisible()

    // 2. Reload / refresh page
    await page.reload()
    await expect(storeTab).toHaveAttribute('data-state', 'active')
    await expect(page.getByRole('heading', { name: 'Sklep z nagrodami' })).toBeVisible()

    // 3. Switch to Zadania tab
    const choresTab = page.getByRole('tab', { name: 'Zadania' })
    await choresTab.click()
    await expect(choresTab).toHaveAttribute('data-state', 'active')
    expect(new URL(page.url()).pathname).toBe('/')

    // 4. Browser back button restores /store
    await page.goBack()
    await expect(storeTab).toHaveAttribute('data-state', 'active')
    await expect(page.getByRole('heading', { name: 'Sklep z nagrodami' })).toBeVisible()
  })

  test('creates a custom reward item via modal, edits it, and deletes it with confirmation dialog', async ({ page }) => {
    await page.goto('/store')

    // 1. Open Add Reward Modal
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

    // 2. Open options menu and click Edit
    await card.getByRole('button', { name: /Opcje nagrody/i }).click()
    await page.getByRole('button', { name: /Edytuj/i }).click()

    // Modal opens with prefilled fields
    await expect(page.getByRole('heading', { name: /Edytuj nagrodę/i })).toBeVisible()
    await page.getByLabel(/Koszt w punktach/i).fill('35')
    await page.getByLabel(/Liczba sztuk/i).fill('4')

    const editPromise = page.waitForResponse(
      (res) => res.url().includes('/api/store/items') && res.status() === 200
    )
    await page.getByRole('button', { name: /Zapisz zmiany/i }).click()
    await editPromise

    await expect(card.getByText('35 pkt')).toBeVisible()
    await expect(card.getByText('Zostało: 4 szt.')).toBeVisible()

    // 3. Open options menu and click Delete
    await card.getByRole('button', { name: /Opcje nagrody/i }).click()
    await page.getByRole('button', { name: /Usuń/i }).click()

    // Delete confirmation dialog
    const deleteDialog = page.getByRole('dialog')
    await expect(deleteDialog.getByRole('heading', { name: /Wycofać nagrodę ze sklepu\?/i })).toBeVisible()

    const deletePromise = page.waitForResponse(
      (res) => res.url().includes('/api/store/items') && res.status() === 204
    )
    await deleteDialog.getByRole('button', { name: /Usuń ze sklepu/i }).click()
    await deletePromise

    await expect(card).not.toBeVisible()
  })

  test('disables purchase button when wallet balance is insufficient', async ({ page }) => {
    // Seed user with 10 pkt
    await seedTestUserWithBalance({
      externalSubjectId: STORE_USER.sub,
      name: STORE_USER.name,
      walletBalance: 10,
    })

    await page.goto('/store')
    await expect(page.getByText(/Twój portfel: 10 pkt/i)).toBeVisible()

    // Create item costing 25 pkt
    const expensiveTitle = `STORE_REWARD_${Date.now()}`
    await page.getByRole('button', { name: /Dodaj nagrodę/i }).click()
    await page.getByLabel(/Nazwa nagrody/i).fill(expensiveTitle)
    await page.getByLabel(/Koszt w punktach/i).fill('25')
    await page.getByRole('button', { name: 'Dodaj nagrodę' }).click()

    const card = page.locator('[data-testid="reward-card"]', { hasText: expensiveTitle })
    await expect(card).toBeVisible()

    // Purchase button should be disabled due to insufficient funds
    const buyButton = card.getByRole('button', { name: /Kup nagrodę/i })
    await expect(buyButton).toBeDisabled()
    await expect(buyButton).toHaveAttribute('title', 'Niewystarczająca liczba punktów w portfelu')
  })

  test('depletes stock to 0 and deactivates reward item upon final purchase', async ({ page }) => {
    // Seed user with 50 pkt
    await seedTestUserWithBalance({
      externalSubjectId: STORE_USER.sub,
      name: STORE_USER.name,
      walletBalance: 50,
    })

    await page.goto('/store')

    // Create reward item with quantity = 1 (single claim)
    const singleTitle = `STORE_REWARD_${Date.now()}`
    await page.getByRole('button', { name: /Dodaj nagrodę/i }).click()
    await page.getByLabel(/Nazwa nagrody/i).fill(singleTitle)
    await page.getByLabel(/Koszt w punktach/i).fill('15')
    await page.getByLabel(/Liczba sztuk/i).fill('1')
    await page.getByRole('button', { name: 'Dodaj nagrodę' }).click()

    const card = page.locator('[data-testid="reward-card"]', { hasText: singleTitle })
    await expect(card).toBeVisible()
    await expect(card.getByText('Zostało: 1 szt.')).toBeVisible()

    // Purchase the single item
    await card.getByRole('button', { name: /Kup nagrodę/i }).click()
    const purchaseDialog = page.getByRole('dialog')
    await purchaseDialog.getByRole('button', { name: /Kupuję nagrodę/i }).click()

    // The item is exhausted (Quantity reached 0, IsActive became false) -> removed from catalog
    await expect(card).not.toBeVisible()

    // The voucher is in Mój portfel
    const voucherCard = page.locator('[data-testid="voucher-card"]', { hasText: singleTitle })
    await expect(voucherCard).toBeVisible()
  })

  test('Golden Journey: Store Loop (direct boot with balance, purchase reward, verify wallet and voucher, redeem voucher)', async ({ page }) => {
    // 1. Boot app with synthetic user who has wallet points (50 pkt pre-seeded in MySQL)
    await seedTestUserWithBalance({
      externalSubjectId: STORE_USER.sub,
      name: STORE_USER.name,
      walletBalance: 50,
    })

    await page.goto('/')
    await expect(page.locator('header').getByText(/50 pkt/)).toBeVisible()

    // 2. Navigate to "Sklep"
    const storeTab = page.getByRole('tab', { name: /Sklep/i })
    await storeTab.click()
    await expect(storeTab).toHaveAttribute('data-state', 'active')
    await expect(page.getByText(/Twój portfel: 50 pkt/i)).toBeVisible()

    // 3. Purchase a reward item
    const rewardTitle = `STORE_REWARD_${Date.now()}`
    await page.getByRole('button', { name: /Dodaj nagrodę/i }).click()
    await page.getByLabel(/Nazwa nagrody/i).fill(rewardTitle)
    await page.getByLabel(/Koszt w punktach/i).fill('20')
    await page.getByLabel(/Liczba sztuk/i).fill('3')
    await page.getByRole('button', { name: 'Dodaj nagrodę' }).click()

    const rewardCard = page.locator('[data-testid="reward-card"]', { hasText: rewardTitle })
    await expect(rewardCard).toBeVisible()

    const buyButton = rewardCard.getByRole('button', { name: /Kup nagrodę/i })
    await expect(buyButton).toBeEnabled()
    await buyButton.click()

    // Purchase confirmation dialog
    const purchaseDialog = page.getByRole('dialog')
    await expect(purchaseDialog.getByRole('heading', { name: /Potwierdź zakup nagrody/i })).toBeVisible()
    await expect(purchaseDialog.getByText(/Czy na pewno chcesz wymienić/i)).toBeVisible()
    await expect(purchaseDialog.getByText('20 pkt')).toBeVisible()

    const purchasePromise = page.waitForResponse(
      (res) => res.url().includes('/purchase') && res.status() === 201
    )
    await purchaseDialog.getByRole('button', { name: /Kupuję nagrodę/i }).click()
    await purchasePromise

    await expect(page.getByRole('dialog')).not.toBeVisible()

    // 4. Verify wallet balance decreases (50 pkt -> 30 pkt)
    await expect(page.getByText(/Twój portfel: 30 pkt/i)).toBeVisible()
    await expect(page.locator('header').getByText(/30 pkt/)).toBeVisible()
    await expect(rewardCard.getByText('Zostało: 2 szt.')).toBeVisible()

    // 5. Navigate to "Portfel" (section "Mój portfel") and verify voucher is Available
    const walletSection = page.locator('section', { hasText: 'Mój portfel' })
    await expect(walletSection).toBeVisible()
    const voucherCard = page.locator('[data-testid="voucher-card"]', { hasText: rewardTitle })
    await expect(voucherCard).toBeVisible()
    await expect(voucherCard.getByText('20 pkt')).toBeVisible()

    // 6. Click "Zrealizuj kupon"
    const redeemButton = voucherCard.getByRole('button', { name: /Zrealizuj kupon/i })
    await expect(redeemButton).toBeEnabled()
    await redeemButton.click()

    // Redeem confirmation dialog
    const redeemDialog = page.getByRole('dialog')
    await expect(redeemDialog.getByRole('heading', { name: /Realizacja kuponu/i })).toBeVisible()
    await expect(redeemDialog.getByText(/Czy na pewno chcesz oznaczyć kupon/i)).toBeVisible()

    const redeemPromise = page.waitForResponse(
      (res) => res.url().includes('/redemption') && res.status() === 200
    )
    await redeemDialog.getByRole('button', { name: 'Zrealizuj kupon' }).click()
    await redeemPromise

    // 7. Verify voucher transitions to Redeemed
    await expect(page.getByRole('dialog')).not.toBeVisible()
    await expect(voucherCard).not.toBeVisible()
    await expect(page.getByText(/Twój portfel jest pusty/i)).toBeVisible()

    // Expand "Historia zrealizowanych" -> voucher is present with 20 pkt snapshot
    const historyHeader = page.locator('button', { hasText: 'Historia zrealizowanych' })
    await historyHeader.click()

    const redeemedCard = page.locator('[data-testid="redeemed-voucher-card"]', { hasText: rewardTitle })
    await expect(redeemedCard).toBeVisible()
    await expect(redeemedCard.getByText('20 pkt')).toBeVisible()
  })
})
