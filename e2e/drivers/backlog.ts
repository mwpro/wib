import { expect, type Page, type Response } from '@playwright/test'

export type CadencePreset =
  | 'Codziennie'
  | 'Co tydzień'
  | 'Co miesiąc'
  | 'Co 3 mies.'
  | 'Co pół roku'
  | 'Co rok'
  | 'Bez terminu'

export interface ChoreDraft {
  title?: string
  description?: string
  cadence?: CadencePreset
  tags?: string[]
}

const isCompletion = (res: Response) => res.url().endsWith('/completion') && res.ok()

export class Backlog {
  constructor(readonly page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { level: 2, name: /Zadania domowe/i })
  }

  get addButton() {
    return this.page.getByRole('button', { name: /Dodaj zadanie/i }).first()
  }

  get cards() {
    return this.page.getByTestId('chore-card')
  }

  get searchInput() {
    return this.page.getByPlaceholder(/Szukaj zadań/i)
  }

  get noSearchResults() {
    return this.page.getByText(/Brak zadań spełniających kryteria wyszukiwania/i)
  }

  tagFilter(name: string) {
    return this.page.getByTestId('chore-filters').getByRole('button', { name, exact: true })
  }

  get allTagsFilter() {
    return this.page.getByTestId('tag-filters').getByRole('button', { name: 'Wszystkie' })
  }

  cadenceFilter(name: CadencePreset) {
    return this.page.getByTestId('cadence-filters').getByRole('button', { name, exact: true })
  }

  clearFiltersButton(activeCount: number) {
    return this.page.getByRole('button', { name: `Wyczyść filtry (${activeCount})` })
  }

  async search(text: string) {
    await this.searchInput.fill(text)
  }

  async openNewChoreForm() {
    await this.addButton.click()
    return new ChoreForm(this.page)
  }

  async addChore(draft: ChoreDraft & { title: string }) {
    const form = await this.openNewChoreForm()
    await form.fill(draft)
    await form.submit()
    const card = this.chore(draft.title)
    await expect(card.root).toBeVisible()
    return card
  }

  chore(title: string) {
    return new ChoreCard(this.page, title)
  }
}

export class ChoreForm {
  constructor(readonly page: Page) {}

  get dialog() {
    return this.page.getByRole('dialog')
  }

  get heading() {
    return this.dialog.getByRole('heading', { name: /Edytuj zadanie|Nowe zadanie/i })
  }

  get titleInput() {
    return this.dialog.locator('#chore-title')
  }

  get submitButton() {
    return this.dialog.getByRole('button', { name: /Utwórz zadanie|Zapisz zmiany/i })
  }

  async fill({ title, description, cadence, tags = [] }: ChoreDraft) {
    if (title !== undefined) await this.titleInput.fill(title)
    if (description !== undefined) await this.dialog.locator('#chore-desc').fill(description)
    if (cadence) await this.dialog.getByRole('button', { name: cadence, exact: true }).click()
    for (const tag of tags) {
      await this.dialog.getByPlaceholder(/Nowy tag/i).fill(tag)
      await this.dialog.getByRole('button', { name: 'Dodaj', exact: true }).click()
    }
  }

  async submit() {
    await this.submitButton.click()
    await expect(this.dialog).toBeHidden()
  }
}

export type ChoreMenuItem = 'Edytuj' | 'Usuń' | 'Historia' | 'Zrobione ponownie'

export class ChoreCard {
  constructor(
    readonly page: Page,
    readonly title: string
  ) {}

  get root() {
    return this.page.getByTestId('chore-card').filter({ hasText: this.title })
  }

  get completeButton() {
    return this.root.getByRole('button', { name: 'Zrobione!' })
  }

  get doneButton() {
    return this.root.getByRole('button', { name: /^Ukończono/ })
  }

  text(value: string) {
    return this.root.getByText(value, { exact: true })
  }

  async complete() {
    const completion = this.page.waitForResponse(isCompletion)
    await this.completeButton.click()
    await completion
  }

  async toggleMenu() {
    await this.root.getByRole('button', { name: /Więcej opcji/i }).click()
  }

  menuItem(name: ChoreMenuItem) {
    return this.root.getByRole('button', { name, exact: true })
  }

  async choose(name: ChoreMenuItem) {
    await this.toggleMenu()
    await this.menuItem(name).click()
  }

  async edit() {
    await this.choose('Edytuj')
    return new ChoreForm(this.page)
  }

  async openDelete() {
    await this.choose('Usuń')
    return this.page.getByRole('dialog')
  }

  async openRepeat() {
    await this.choose('Zrobione ponownie')
    return new RepeatDialog(this.page)
  }

  async openHistory() {
    await this.choose('Historia')
    return new HistoryModal(this.page)
  }
}

export class RepeatDialog {
  constructor(readonly page: Page) {}

  get root() {
    return this.page.getByRole('dialog', { name: /Zrobione ponownie\?/i })
  }

  async cancel() {
    await this.root.getByRole('button', { name: 'Anuluj' }).click()
    await expect(this.root).toBeHidden()
  }

  async confirm() {
    const completion = this.page.waitForResponse(isCompletion)
    await this.root.getByRole('button', { name: /Zapisz ponownie/i }).click()
    await completion
    await expect(this.root).toBeHidden()
  }
}

export class HistoryModal {
  constructor(readonly page: Page) {}

  get root() {
    return this.page.getByTestId('chore-history-modal')
  }

  get total() {
    return this.root.getByTestId('history-total')
  }

  get onTime() {
    return this.root.getByTestId('history-on-time')
  }

  outcome(urgency: 'Fresh' | 'DueSoon' | 'Overdue' | 'Neglected') {
    return this.root.getByTestId(`history-outcome-${urgency}`)
  }

  get memberShares() {
    return this.root.getByTestId('history-member-share')
  }

  get monthHeaders() {
    return this.root.getByTestId('history-month-header')
  }

  get rows() {
    return this.root.getByTestId('history-row')
  }

  row(index: number) {
    const row = index < 0 ? this.rows.last() : this.rows.nth(index)
    return {
      outcome: row.getByTestId('history-row-outcome'),
      gap: row.getByTestId('history-row-gap'),
    }
  }

  get loadMoreButton() {
    return this.root.getByTestId('history-load-more')
  }

  get emptyState() {
    return this.page.getByText('To zadanie nie zostało jeszcze wykonane.')
  }

  async close() {
    await this.page.keyboard.press('Escape')
    await expect(this.root).toHaveCount(0)
  }
}
