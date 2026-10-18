import { test, expect } from '../fixtures'

test.describe('Store & Voucher Wallet UI Tests', () => {
  test('renders store tab sections and empty states', async ({ app, store, household, page }) => {
    await (await household.member({ name: 'Ania' })).actAs(page)
    await app.goto()

    await app.openTab('Sklep')
    await expect(app.tab('Sklep')).toHaveAttribute('data-state', 'active')

    await expect(store.heading).toBeVisible()
    await expect(store.wallet(0)).toBeVisible()
    await expect(store.addButton).toBeVisible()
    await expect(store.section('Mój portfel')).toBeVisible()
    await expect(store.emptyWallet).toBeVisible()
    await expect(store.section('Dostępne nagrody')).toBeVisible()
    await expect(store.section('Historia zrealizowanych')).toBeVisible()
  })

  test('persists active store tab in URL and restores upon refresh or direct navigation', async ({ app, store, page }) => {
    await app.goto('/store')
    await expect(app.tab('Sklep')).toHaveAttribute('data-state', 'active')
    await expect(store.heading).toBeVisible()

    await page.reload()
    await expect(app.tab('Sklep')).toHaveAttribute('data-state', 'active')
    await expect(store.heading).toBeVisible()

    await app.openTab('Zadania')
    await expect(app.tab('Zadania')).toHaveAttribute('data-state', 'active')
    expect(new URL(page.url()).pathname).toBe('/')

    await page.goBack()
    await expect(app.tab('Sklep')).toHaveAttribute('data-state', 'active')
    await expect(store.heading).toBeVisible()
  })

  test('creates a custom reward item via modal, edits it, and deletes it with confirmation dialog', async ({ app, store }) => {
    await app.goto('/store')

    const reward = await store.addReward({
      title: 'Masaż',
      cost: 25,
      quantity: 2,
      description: 'Relaksujący masaż pleców',
    })
    await expect(reward.text('25 pkt')).toBeVisible()
    await expect(reward.text('Zostało: 2 szt.')).toBeVisible()
    await expect(reward.text('Relaksujący masaż pleców')).toBeVisible()

    await reward.edit({ cost: 35, quantity: 4 })
    await expect(reward.text('35 pkt')).toBeVisible()
    await expect(reward.text('Zostało: 4 szt.')).toBeVisible()

    const deleteDialog = await reward.openDelete()
    await expect(deleteDialog.root.getByRole('heading', { name: /Wycofać nagrodę ze sklepu\?/i })).toBeVisible()
    await deleteDialog.confirm()
    await expect(reward.root).toBeHidden()
  })

  test('disables purchase button when wallet balance is insufficient', async ({ app, store, household, page }) => {
    await (await household.member({ name: 'Ania', wallet: 10 })).actAs(page)

    await app.goto('/store')
    await expect(store.wallet(10)).toBeVisible()

    const reward = await store.addReward({ title: 'Wyjście solo', cost: 25 })
    await expect(reward.buyButton).toBeDisabled()
    await expect(reward.buyButton).toHaveAttribute('title', 'Niewystarczająca liczba punktów w portfelu')
  })

  test('depletes stock to 0 and deactivates reward item upon final purchase', async ({ app, store, household, page }) => {
    await (await household.member({ name: 'Ania', wallet: 50 })).actAs(page)
    await app.goto('/store')

    const reward = await store.addReward({ title: 'Kupno książki', cost: 15, quantity: 1 })
    await expect(reward.text('Zostało: 1 szt.')).toBeVisible()

    await reward.buy()

    await expect(reward.root).toBeHidden()
    await expect(store.voucher('Kupno książki').root).toBeVisible()
  })

  test('Golden Journey: Store Loop (direct boot with balance, purchase reward, verify wallet and voucher, redeem voucher)', async ({ app, store, household, page }) => {
    await (await household.member({ name: 'Ania', wallet: 50 })).actAs(page)
    await app.goto()
    await expect(app.points(50)).toBeVisible()

    await app.openTab('Sklep')
    await expect(app.tab('Sklep')).toHaveAttribute('data-state', 'active')
    await expect(store.wallet(50)).toBeVisible()

    const reward = await store.addReward({ title: 'Masaż', cost: 20, quantity: 3 })
    await expect(reward.buyButton).toBeEnabled()

    const purchase = await reward.openPurchase()
    await expect(purchase.root.getByRole('heading', { name: /Potwierdź zakup nagrody/i })).toBeVisible()
    await expect(purchase.root.getByText(/Czy na pewno chcesz wymienić/i)).toBeVisible()
    await expect(purchase.root.getByText('20 pkt')).toBeVisible()
    await purchase.confirm()

    await expect(store.wallet(30)).toBeVisible()
    await expect(app.points(30)).toBeVisible()
    await expect(reward.text('Zostało: 2 szt.')).toBeVisible()

    await expect(store.section('Mój portfel')).toBeVisible()
    const voucher = store.voucher('Masaż')
    await expect(voucher.text('20 pkt')).toBeVisible()
    await expect(voucher.redeemButton).toBeEnabled()

    const redemption = await voucher.openRedeem()
    await expect(redemption.root.getByRole('heading', { name: /Realizacja kuponu/i })).toBeVisible()
    await expect(redemption.root.getByText(/Czy na pewno chcesz oznaczyć kupon/i)).toBeVisible()
    await redemption.confirm()

    await expect(voucher.root).toBeHidden()
    await expect(store.emptyWallet).toBeVisible()

    await store.showRedeemedHistory()
    const redeemed = store.redeemedVoucher('Masaż')
    await expect(redeemed).toBeVisible()
    await expect(redeemed.getByText('20 pkt', { exact: true })).toBeVisible()
  })
})
