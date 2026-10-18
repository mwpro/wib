import { expect, type Page, type Response } from '@playwright/test'

export interface RewardDraft {
  title?: string
  cost?: number
  quantity?: number
  description?: string
}

const respondsWith = (method: string, path: string, status: number) => (res: Response) =>
  res.request().method() === method && res.url().includes(path) && res.status() === status

export class Store {
  constructor(readonly page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { name: 'Sklep z nagrodami' })
  }

  get addButton() {
    return this.page.getByRole('button', { name: /Dodaj nagrodę/i })
  }

  wallet(points: number) {
    return this.page.getByText(`Twój portfel: ${points} pkt`, { exact: true })
  }

  get emptyWallet() {
    return this.page.getByText(/Twój portfel jest pusty/i)
  }

  section(name: 'Mój portfel' | 'Dostępne nagrody' | 'Historia zrealizowanych') {
    return this.page.getByRole('heading', { name })
  }

  async addReward(draft: RewardDraft & { title: string; cost: number }) {
    await this.addButton.click()
    const form = new RewardForm(this.page)
    await form.fill(draft)
    await form.submit()
    const card = this.reward(draft.title)
    await expect(card.root).toBeVisible()
    return card
  }

  reward(title: string) {
    return new RewardCard(this.page, title)
  }

  voucher(title: string) {
    return new VoucherCard(this.page, title)
  }

  redeemedVoucher(title: string) {
    return this.page.getByTestId('redeemed-voucher-card').filter({ hasText: title })
  }

  async showRedeemedHistory() {
    await this.page.locator('button', { hasText: 'Historia zrealizowanych' }).click()
  }
}

export class RewardForm {
  constructor(readonly page: Page) {}

  get dialog() {
    return this.page.getByRole('dialog')
  }

  async fill({ title, cost, quantity, description }: RewardDraft) {
    if (title !== undefined) await this.dialog.getByLabel(/Nazwa nagrody/i).fill(title)
    if (cost !== undefined) await this.dialog.getByLabel(/Koszt w punktach/i).fill(String(cost))
    if (quantity !== undefined) await this.dialog.getByLabel(/Liczba sztuk/i).fill(String(quantity))
    if (description !== undefined) await this.dialog.getByLabel(/Opis nagrody/i).fill(description)
  }

  async submit() {
    const saved = this.page.waitForResponse(
      (res) =>
        respondsWith('POST', '/api/store/items', 201)(res) || respondsWith('PUT', '/api/store/items/', 200)(res)
    )
    await this.dialog.getByRole('button', { name: /^(Dodaj nagrodę|Zapisz zmiany)$/ }).click()
    await saved
    await expect(this.dialog).toBeHidden()
  }
}

export class ConfirmDialog {
  constructor(
    readonly page: Page,
    private readonly confirmLabel: string | RegExp,
    private readonly settled: (res: Response) => boolean
  ) {}

  get root() {
    return this.page.getByRole('dialog')
  }

  async confirm() {
    const response = this.page.waitForResponse(this.settled)
    await this.root.getByRole('button', { name: this.confirmLabel }).click()
    await response
    await expect(this.root).toBeHidden()
  }
}

export class RewardCard {
  constructor(
    readonly page: Page,
    readonly title: string
  ) {}

  get root() {
    return this.page.getByTestId('reward-card').filter({ hasText: this.title })
  }

  get buyButton() {
    return this.root.getByRole('button', { name: /Kup nagrodę/i })
  }

  text(value: string) {
    return this.root.getByText(value, { exact: true })
  }

  async openPurchase() {
    await this.buyButton.click()
    return new ConfirmDialog(this.page, /Kupuję nagrodę/i, respondsWith('POST', '/purchase', 201))
  }

  async buy() {
    await (await this.openPurchase()).confirm()
  }

  private async choose(name: 'Edytuj' | 'Usuń') {
    await this.root.getByRole('button', { name: /Opcje nagrody/i }).click()
    await this.page.getByRole('button', { name, exact: true }).click()
  }

  async edit(changes: RewardDraft) {
    await this.choose('Edytuj')
    const form = new RewardForm(this.page)
    await expect(form.dialog.getByRole('heading', { name: /Edytuj nagrodę/i })).toBeVisible()
    await form.fill(changes)
    await form.submit()
  }

  async openDelete() {
    await this.choose('Usuń')
    return new ConfirmDialog(this.page, /Usuń ze sklepu/i, respondsWith('DELETE', '/api/store/items/', 204))
  }
}

export class VoucherCard {
  constructor(
    readonly page: Page,
    readonly title: string
  ) {}

  get root() {
    return this.page.getByTestId('voucher-card').filter({ hasText: this.title })
  }

  get redeemButton() {
    return this.root.getByRole('button', { name: /Zrealizuj kupon/i })
  }

  text(value: string) {
    return this.root.getByText(value, { exact: true })
  }

  async openRedeem() {
    await this.redeemButton.click()
    return new ConfirmDialog(this.page, 'Zrealizuj kupon', respondsWith('POST', '/redemption', 200))
  }

  async redeem() {
    await (await this.openRedeem()).confirm()
  }
}
