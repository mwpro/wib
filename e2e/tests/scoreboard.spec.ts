import { test, expect } from '../fixtures'
import { App } from '../drivers/app'
import { Backlog } from '../drivers/backlog'
import { Scoreboard } from '../drivers/scoreboard'

test.describe('Scoreboard & Activity Stream UI Tests', () => {
  test.beforeEach(async ({ household, page }) => {
    const kasia = await household.member({ name: 'Kasia' })
    await kasia.actAs(page)
  })

  test('renders scoreboard tab elements and month navigator', async ({ app, scoreboard }) => {
    await app.goto()
    await app.openTab('Kto jest lepszy?')
    await expect(app.tab('Kto jest lepszy?')).toHaveAttribute('data-state', 'active')

    await expect(scoreboard.monthNavigator).toBeVisible()
    await expect(scoreboard.nextMonthButton).toBeDisabled()
    await expect(scoreboard.workShare).toBeVisible()
    await expect(scoreboard.podium).toBeVisible()
    await expect(scoreboard.activityStream).toBeVisible()
    await expect(scoreboard.lifetimeStats).toBeVisible()
  })

  test('Golden Journey: multi-user chore competition and proportional work share update', async ({ app, backlog, household, browser }) => {
    await app.goto()
    const vacuuming = await backlog.addChore({ title: 'Odkurzanie' })
    await vacuuming.complete()
    await expect(vacuuming.doneButton).toBeVisible()

    const tomek = await household.member({ name: 'Tomek' })
    const contextB = await browser.newContext()
    await tomek.actAs(contextB)
    const pageB = await contextB.newPage()

    try {
      const appB = new App(pageB)
      const backlogB = new Backlog(pageB)
      await appB.goto()

      for (const title of ['Zmywanie', 'Pranie']) {
        const chore = await backlogB.addChore({ title })
        await chore.complete()
        await expect(chore.doneButton).toBeVisible()
      }

      await appB.openTab('Kto jest lepszy?')
      await expect(appB.tab('Kto jest lepszy?')).toHaveAttribute('data-state', 'active')

      const scoreboardB = new Scoreboard(pageB)
      await expect(scoreboardB.workShare).toContainText('Łącznie: 3 pkt')
      await expect(scoreboardB.workShareSegment('Kasia')).toHaveAttribute('title', 'Kasia: 33.3%')
      await expect(scoreboardB.workShareSegment('Tomek')).toHaveAttribute('title', 'Tomek: 66.7%')
      for (const text of ['Kasia', '33.3%', '(1 pkt)', 'Tomek', '66.7%', '(2 pkt)']) {
        await expect(scoreboardB.workShare).toContainText(text)
      }

      for (const text of ['Tomek', '🥇', 'Kasia', '🥈']) {
        await expect(scoreboardB.podium).toContainText(text)
      }
      await expect(scoreboardB.podium).toContainText(/2 zadań/i)
      await expect(scoreboardB.podium).toContainText(/1 zadanie/i)

      for (const text of ['Pranie', 'Zmywanie', 'Odkurzanie', 'Tomek', 'Kasia']) {
        await expect(scoreboardB.activityStream).toContainText(text)
      }
    } finally {
      await contextB.close()
    }
  })

  test('supports navigating to previous month and back', async ({ app, scoreboard }) => {
    await app.goto()
    await app.openTab('Kto jest lepszy?')

    await expect(scoreboard.nextMonthButton).toBeDisabled()
    await scoreboard.previousMonthButton.click()
    await expect(scoreboard.nextMonthButton).toBeEnabled()
    await scoreboard.nextMonthButton.click()
    await expect(scoreboard.nextMonthButton).toBeDisabled()
  })

  test('persists active tab in URL and restores upon refresh or direct navigation', async ({ app, scoreboard, page }) => {
    await app.goto('/scoreboard')
    await expect(app.tab('Kto jest lepszy?')).toHaveAttribute('data-state', 'active')
    await expect(scoreboard.workShare).toBeVisible()

    await page.reload()
    await expect(app.tab('Kto jest lepszy?')).toHaveAttribute('data-state', 'active')
    await expect(scoreboard.workShare).toBeVisible()

    await app.openTab('Zadania')
    await expect(app.tab('Zadania')).toHaveAttribute('data-state', 'active')
    expect(new URL(page.url()).pathname).toBe('/')

    await page.goBack()
    await expect(app.tab('Kto jest lepszy?')).toHaveAttribute('data-state', 'active')
    await expect(scoreboard.workShare).toBeVisible()
  })
})
