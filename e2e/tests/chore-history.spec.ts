import { test, expect, type SeededCompletion } from '../fixtures'

// Gaps of a 7-day chore and the outcome each earns; repeated 5x gives 25 completions over 180 days.
const PATTERN: Pick<SeededCompletion, 'daysSincePrevious' | 'urgencyAtCompletion'>[] = [
  { daysSincePrevious: 5, urgencyAtCompletion: 'Fresh' },
  { daysSincePrevious: 7, urgencyAtCompletion: 'DueSoon' },
  { daysSincePrevious: 8, urgencyAtCompletion: 'Overdue' },
  { daysSincePrevious: 10, urgencyAtCompletion: 'Neglected' },
  { daysSincePrevious: 6, urgencyAtCompletion: 'DueSoon' },
]

test.describe('Chore completion history', () => {
  test('shows summary, month groups and outcomes across pages, and records a new completion', async ({ app, backlog, household }) => {
    const ania = await household.member({ name: 'Ania' })
    const tomek = await household.member({ name: 'Tomek' })

    const completions: SeededCompletion[] = Array.from({ length: 25 }, (_, i) => ({
      ...PATTERN[i % PATTERN.length],
      by: i % 2 === 0 ? ania : tomek,
    }))
    // Created 186 days ago: the last seeded completion (day 180) is 6 days old, so completing today is "W terminie".
    await household.choreWithHistory({ title: 'Odkurzanie', cadenceDays: 7, createdDaysAgo: 186, completions })

    await app.goto()
    const chore = backlog.chore('Odkurzanie')
    const history = await chore.openHistory()
    await expect(history.root).toBeVisible()
    await expect(history.root.getByText('Odkurzanie')).toBeVisible()

    await expect(history.total).toHaveText('25')
    await expect(history.onTime).toHaveText('60% na czas')
    await expect(history.outcome('Fresh')).toHaveText(/Przed czasem\s*20%\s*\(5\)/)
    await expect(history.outcome('DueSoon')).toHaveText(/W terminie\s*40%\s*\(10\)/)
    await expect(history.outcome('Overdue')).toHaveText(/Z opóźnieniem\s*20%\s*\(5\)/)
    await expect(history.outcome('Neglected')).toHaveText(/Mocno spóźnione\s*20%\s*\(5\)/)
    await expect(history.memberShares).toHaveCount(2)
    await expect(history.memberShares.nth(0)).toHaveText(/Ania\s*52%\s*\(13\)/)
    await expect(history.memberShares.nth(1)).toHaveText(/Tomek\s*48%\s*\(12\)/)

    await expect(history.rows).toHaveCount(20)
    expect(await history.monthHeaders.count()).toBeGreaterThanOrEqual(2)
    await expect(history.row(0).outcome).toHaveText('W terminie')
    await expect(history.row(0).gap).toContainText('po 6 dniach')
    await expect(history.row(1).outcome).toHaveText('Mocno spóźnione')
    await expect(history.row(2).outcome).toHaveText('Z opóźnieniem')
    await expect(history.row(4).outcome).toHaveText('Przed czasem')

    await history.loadMoreButton.click()
    await expect(history.rows).toHaveCount(25)
    await expect(history.row(-1).gap).toContainText('5 dni od utworzenia')
    await expect(history.row(-1).outcome).toHaveText('Przed czasem')
    await expect(history.loadMoreButton).toHaveCount(0)

    await history.close()
    await chore.complete()
    await expect(chore.doneButton).toBeVisible()

    await chore.openHistory()
    await expect(history.total).toHaveText('26')
    await expect(history.row(0).outcome).toHaveText('W terminie')
    await expect(history.row(0).gap).toContainText('po 6 dniach')
    await expect(history.onTime).toHaveText('62% na czas')
    await expect(history.memberShares).toHaveCount(3)
  })

  test('shows an empty state for a chore that was never done', async ({ app, backlog, household }) => {
    await household.choreWithHistory({ title: 'Nigdy', cadenceDays: 30, createdDaysAgo: 3, completions: [] })

    await app.goto()
    const history = await backlog.chore('Nigdy').openHistory()

    await expect(history.emptyState).toBeVisible()
  })
})
