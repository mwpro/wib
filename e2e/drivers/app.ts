import type { Page } from '@playwright/test'

export type TabName = 'Zadania' | 'Kto jest lepszy?' | 'Sklep'

export class App {
  constructor(readonly page: Page) {}

  get header() {
    return this.page.locator('header')
  }

  get bottomNav() {
    return this.page.locator('nav')
  }

  async goto(path = '/') {
    await this.page.goto(path)
  }

  tab(name: TabName) {
    return this.page.getByRole('tab', { name, exact: true })
  }

  async openTab(name: TabName) {
    await this.tab(name).click()
  }

  points(n: number) {
    return this.header.getByText(`${n} pkt`, { exact: true })
  }
}
