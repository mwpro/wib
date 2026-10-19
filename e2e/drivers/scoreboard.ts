import type { Page } from '@playwright/test'

export class Scoreboard {
  constructor(readonly page: Page) {}

  get monthNavigator() {
    return this.page.getByTestId('month-navigator')
  }

  get previousMonthButton() {
    return this.monthNavigator.getByRole('button', { name: /Poprzedni miesiąc/i })
  }

  get nextMonthButton() {
    return this.monthNavigator.getByRole('button', { name: /Następny miesiąc/i })
  }

  get workShare() {
    return this.page.getByTestId('work-share-card')
  }

  workShareSegment(memberName: string) {
    return this.workShare.locator(`div[title*="${memberName}"]`)
  }

  get podium() {
    return this.page.getByTestId('monthly-podium')
  }

  get activityStream() {
    return this.page.getByTestId('activity-stream')
  }

  get lifetimeStats() {
    return this.page.getByTestId('lifetime-stats')
  }
}
