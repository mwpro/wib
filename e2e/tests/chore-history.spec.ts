import { test, expect } from '@playwright/test'
import {
  resetTestUser,
  cleanupTestChores,
  seedTestUserWithBalance,
  seedChoreWithHistory,
  type SeededCompletion,
} from '../helpers/db'

// Gaps of a 7-day chore and the outcome each earns; repeated 5x gives 25 completions over 180 days.
const PATTERN: Pick<SeededCompletion, 'daysSincePrevious' | 'urgencyAtCompletion'>[] = [
  { daysSincePrevious: 5, urgencyAtCompletion: 'Fresh' },
  { daysSincePrevious: 7, urgencyAtCompletion: 'DueSoon' },
  { daysSincePrevious: 8, urgencyAtCompletion: 'Overdue' },
  { daysSincePrevious: 10, urgencyAtCompletion: 'Neglected' },
  { daysSincePrevious: 6, urgencyAtCompletion: 'DueSoon' },
]

test.describe.serial('Chore completion history', () => {
  test.beforeEach(async () => {
    await resetTestUser()
    await cleanupTestChores('E2E_')
  })

  test.afterAll(async () => {
    await cleanupTestChores('E2E_')
  })

  test('shows summary, month groups and outcomes across pages, and records a new completion', async ({ page }) => {
    const ania = await seedTestUserWithBalance({ externalSubjectId: 'auth0|e2e-history-ania', name: 'E2E Ania' })
    const tomek = await seedTestUserWithBalance({ externalSubjectId: 'auth0|e2e-history-tomek', name: 'E2E Tomek' })

    const completions: SeededCompletion[] = Array.from({ length: 25 }, (_, i) => ({
      ...PATTERN[i % PATTERN.length],
      memberId: i % 2 === 0 ? ania : tomek,
    }))
    // Created 186 days ago: the last seeded completion (day 180) is 6 days old, so completing today is "W terminie".
    const chore = await seedChoreWithHistory({
      title: `E2E_Odkurzanie_${Date.now()}`,
      cadenceDays: 7,
      createdDaysAgo: 186,
      completions,
    })

    await page.goto('/')
    const choreCard = page.locator('[data-testid="chore-card"]', { hasText: chore.title })
    await expect(choreCard).toBeVisible()

    // Open history from the card menu
    await choreCard.getByRole('button', { name: /Więcej opcji/i }).click()
    await page.getByRole('button', { name: 'Historia' }).click()

    const modal = page.getByTestId('chore-history-modal')
    await expect(modal).toBeVisible()
    await expect(modal.getByText(chore.title)).toBeVisible()

    // Summary spans all 25 completions, not just the first page
    await expect(modal.getByTestId('history-total')).toHaveText('25')
    await expect(modal.getByTestId('history-on-time')).toHaveText('60% na czas')
    await expect(modal.getByTestId('history-outcome-Fresh')).toHaveText(/Przed czasem\s*20%\s*\(5\)/)
    await expect(modal.getByTestId('history-outcome-DueSoon')).toHaveText(/W terminie\s*40%\s*\(10\)/)
    await expect(modal.getByTestId('history-outcome-Overdue')).toHaveText(/Z opóźnieniem\s*20%\s*\(5\)/)
    await expect(modal.getByTestId('history-outcome-Neglected')).toHaveText(/Mocno spóźnione\s*20%\s*\(5\)/)
    const shares = modal.getByTestId('history-member-share')
    await expect(shares).toHaveCount(2)
    await expect(shares.nth(0)).toHaveText(/E2E Ania\s*52%\s*\(13\)/)
    await expect(shares.nth(1)).toHaveText(/E2E Tomek\s*48%\s*\(12\)/)

    // First page: 20 newest rows grouped under several month headers, newest first
    const rows = modal.getByTestId('history-row')
    await expect(rows).toHaveCount(20)
    expect(await modal.getByTestId('history-month-header').count()).toBeGreaterThanOrEqual(2)
    await expect(rows.first().getByTestId('history-row-outcome')).toHaveText('W terminie')
    await expect(rows.first().getByTestId('history-row-gap')).toContainText('po 6 dniach')
    await expect(rows.nth(1).getByTestId('history-row-outcome')).toHaveText('Mocno spóźnione')
    await expect(rows.nth(2).getByTestId('history-row-outcome')).toHaveText('Z opóźnieniem')
    await expect(rows.nth(4).getByTestId('history-row-outcome')).toHaveText('Przed czasem')

    // Second page: remaining 5, oldest measured from creation, no more pages
    await modal.getByTestId('history-load-more').click()
    await expect(rows).toHaveCount(25)
    await expect(rows.last().getByTestId('history-row-gap')).toContainText('5 dni od utworzenia')
    await expect(rows.last().getByTestId('history-row-outcome')).toHaveText('Przed czasem')
    await expect(modal.getByTestId('history-load-more')).toHaveCount(0)

    // Complete the chore and verify the new completion is judged and stored
    await page.keyboard.press('Escape')
    await expect(modal).toHaveCount(0)
    await choreCard.getByRole('button', { name: /Zrobione!/i }).click()
    await expect(choreCard.getByRole('button', { name: /Ukończono/i })).toBeVisible()

    await choreCard.getByRole('button', { name: /Więcej opcji/i }).click()
    await page.getByRole('button', { name: 'Historia' }).click()
    await expect(modal.getByTestId('history-total')).toHaveText('26')
    await expect(rows.first().getByTestId('history-row-outcome')).toHaveText('W terminie')
    await expect(rows.first().getByTestId('history-row-gap')).toContainText('po 6 dniach')
    await expect(modal.getByTestId('history-on-time')).toHaveText('62% na czas')
    await expect(modal.getByTestId('history-member-share')).toHaveCount(3)
  })

  test('shows an empty state for a chore that was never done', async ({ page }) => {
    const chore = await seedChoreWithHistory({
      title: `E2E_Nigdy_${Date.now()}`,
      cadenceDays: 30,
      createdDaysAgo: 3,
      completions: [],
    })

    await page.goto('/')
    const choreCard = page.locator('[data-testid="chore-card"]', { hasText: chore.title })
    await choreCard.getByRole('button', { name: /Więcej opcji/i }).click()
    await page.getByRole('button', { name: 'Historia' }).click()

    await expect(page.getByText('To zadanie nie zostało jeszcze wykonane.')).toBeVisible()
  })
})
