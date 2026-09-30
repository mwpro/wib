import { test, expect } from '@playwright/test'
import { resetTestUser, cleanupTestChores } from '../helpers/db'

const SB_USER_A = {
  sub: 'auth0|test-sb-user-a',
  name: 'Kasia',
  email: 'kasia@test.com',
}

const SB_USER_B = {
  sub: 'auth0|test-sb-user-b',
  name: 'Tomek',
  email: 'tomek@test.com',
}

test.describe.serial('Scoreboard & Activity Stream UI Tests', () => {
  test.beforeEach(async ({ page }) => {
    await resetTestUser(SB_USER_A.sub)
    await resetTestUser(SB_USER_B.sub)
    await cleanupTestChores('SB_CHORE_')
    await page.addInitScript((user) => {
      localStorage.setItem('wib_test_user', JSON.stringify(user))
    }, SB_USER_A)
  })

  test.afterAll(async () => {
    await resetTestUser(SB_USER_A.sub)
    await resetTestUser(SB_USER_B.sub)
    await cleanupTestChores('SB_CHORE_')
  })

  test('renders scoreboard tab elements and month navigator', async ({ page }) => {
    await page.goto('/')

    // Switch to Scoreboard tab ("Kto jest lepszy?")
    const scoreboardTab = page.getByRole('tab', { name: /Kto jest lepszy\?/i })
    await scoreboardTab.click()
    await expect(scoreboardTab).toHaveAttribute('data-state', 'active')

    // Month navigator is visible and next button is disabled for current month
    const navigator = page.locator('[data-testid="month-navigator"]')
    await expect(navigator).toBeVisible()
    const nextBtn = navigator.getByRole('button', { name: /Następny miesiąc/i })
    await expect(nextBtn).toBeDisabled()

    // Core scoreboard sections are rendered
    await expect(page.locator('[data-testid="work-share-card"]')).toBeVisible()
    await expect(page.locator('[data-testid="monthly-podium"]')).toBeVisible()
    await expect(page.locator('[data-testid="activity-stream"]')).toBeVisible()
    await expect(page.locator('[data-testid="lifetime-stats"]')).toBeVisible()
  })

  test('Golden Journey: multi-user chore competition and proportional work share update', async ({ page, browser }) => {
    // 1. Boot app with synthetic User A (Kasia) and complete a chore
    await page.goto('/')

    const choreTitleA = `SB_CHORE_A_${Date.now()}`
    const quickAddFormA = page.locator('[data-testid="quick-add-chore"]')
    await quickAddFormA.getByPlaceholder(/Dodaj nowe zadanie/i).fill(choreTitleA)
    await quickAddFormA.getByRole('button', { name: 'Dodaj' }).click()

    const choreCardA = page.locator('[data-testid="chore-card"]', { hasText: choreTitleA })
    await expect(choreCardA).toBeVisible()

    const completionPromiseA = page.waitForResponse(
      (res) => res.url().includes('/completion') && res.status() === 200
    )
    await choreCardA.getByRole('button', { name: /Zrobione!/i }).click()
    await completionPromiseA
    await expect(choreCardA.getByRole('button', { name: /Ukończono/i })).toBeVisible()

    // 2. Switch / authenticate as synthetic User B (Tomek) in a separate browser context and complete chores
    const contextB = await browser.newContext()
    await contextB.addInitScript((user) => {
      localStorage.setItem('wib_test_user', JSON.stringify(user))
    }, SB_USER_B)
    const pageB = await contextB.newPage()

    try {
      await pageB.goto('/')

      // Tomek completes 2 chores to test asymmetric split (1 pkt vs 2 pkt => 33.3% vs 66.7%)
      const choreTitleB1 = `SB_CHORE_B1_${Date.now()}`
      const quickAddFormB = pageB.locator('[data-testid="quick-add-chore"]')
      await quickAddFormB.getByPlaceholder(/Dodaj nowe zadanie/i).fill(choreTitleB1)
      await quickAddFormB.getByRole('button', { name: 'Dodaj' }).click()

      const choreCardB1 = pageB.locator('[data-testid="chore-card"]', { hasText: choreTitleB1 })
      await expect(choreCardB1).toBeVisible()

      const completionPromiseB1 = pageB.waitForResponse(
        (res) => res.url().includes('/completion') && res.status() === 200
      )
      await choreCardB1.getByRole('button', { name: /Zrobione!/i }).click()
      await completionPromiseB1
      await expect(choreCardB1.getByRole('button', { name: /Ukończono/i })).toBeVisible()

      const choreTitleB2 = `SB_CHORE_B2_${Date.now()}`
      await quickAddFormB.getByPlaceholder(/Dodaj nowe zadanie/i).fill(choreTitleB2)
      await quickAddFormB.getByRole('button', { name: 'Dodaj' }).click()

      const choreCardB2 = pageB.locator('[data-testid="chore-card"]', { hasText: choreTitleB2 })
      await expect(choreCardB2).toBeVisible()

      const completionPromiseB2 = pageB.waitForResponse(
        (res) => res.url().includes('/completion') && res.status() === 200
      )
      await choreCardB2.getByRole('button', { name: /Zrobione!/i }).click()
      await completionPromiseB2
      await expect(choreCardB2.getByRole('button', { name: /Ukończono/i })).toBeVisible()

      // 3. Navigate to "Kto jest lepszy?" view in User B context
      const scoreboardTabB = pageB.getByRole('tab', { name: /Kto jest lepszy\?/i })
      await scoreboardTabB.click()
      await expect(scoreboardTabB).toHaveAttribute('data-state', 'active')

      // 4. Verify segmented progress bar updates to reflect proportional split (Kasia 33.3%, Tomek 66.7%)
      const workShareCardB = pageB.locator('[data-testid="work-share-card"]')
      await expect(workShareCardB).toBeVisible()
      await expect(workShareCardB).toContainText('Łącznie: 3 pkt')

      // Segment widths in progress bar
      const kasiaSegment = workShareCardB.locator('div[title*="Kasia"]')
      await expect(kasiaSegment).toBeVisible()
      await expect(kasiaSegment).toHaveAttribute('title', 'Kasia: 33.3%')

      const tomekSegment = workShareCardB.locator('div[title*="Tomek"]')
      await expect(tomekSegment).toBeVisible()
      await expect(tomekSegment).toHaveAttribute('title', 'Tomek: 66.7%')

      // Legend entries with percentage and points
      await expect(workShareCardB).toContainText('Kasia')
      await expect(workShareCardB).toContainText('33.3%')
      await expect(workShareCardB).toContainText('(1 pkt)')

      await expect(workShareCardB).toContainText('Tomek')
      await expect(workShareCardB).toContainText('66.7%')
      await expect(workShareCardB).toContainText('(2 pkt)')

      // 5. Verify Monthly Podium reflects ranking (Tomek #1, Kasia #2)
      const podiumB = pageB.locator('[data-testid="monthly-podium"]')
      await expect(podiumB).toBeVisible()
      await expect(podiumB).toContainText('Tomek')
      await expect(podiumB).toContainText('🥇')
      await expect(podiumB).toContainText(/2 zadań/i)
      await expect(podiumB).toContainText('Kasia')
      await expect(podiumB).toContainText('🥈')
      await expect(podiumB).toContainText(/1 zadanie/i)

      // 6. Verify Chronological Activity Stream has entries for both users
      const activityStreamB = pageB.locator('[data-testid="activity-stream"]')
      await expect(activityStreamB).toBeVisible()
      await expect(activityStreamB).toContainText(choreTitleB2)
      await expect(activityStreamB).toContainText(choreTitleB1)
      await expect(activityStreamB).toContainText(choreTitleA)
      await expect(activityStreamB).toContainText('Tomek')
      await expect(activityStreamB).toContainText('Kasia')
    } finally {
      await contextB.close()
    }
  })

  test('supports navigating to previous month and back', async ({ page }) => {
    await page.goto('/')

    // Switch to scoreboard tab
    await page.getByRole('tab', { name: /Kto jest lepszy\?/i }).click()

    const navigator = page.locator('[data-testid="month-navigator"]')
    const prevBtn = navigator.getByRole('button', { name: /Poprzedni miesiąc/i })
    const nextBtn = navigator.getByRole('button', { name: /Następny miesiąc/i })

    // Initially next is disabled
    await expect(nextBtn).toBeDisabled()

    // Navigate to previous month
    await prevBtn.click()

    // Next button should now be enabled
    await expect(nextBtn).toBeEnabled()

    // Navigate back to current month
    await nextBtn.click()

    // Next button is disabled again
    await expect(nextBtn).toBeDisabled()
  })

  test('persists active tab in URL and restores upon refresh or direct navigation', async ({ page }) => {
    // 1. Direct navigation to /scoreboard
    await page.goto('/scoreboard')

    const scoreboardTab = page.getByRole('tab', { name: /Kto jest lepszy\?/i })
    await expect(scoreboardTab).toHaveAttribute('data-state', 'active')
    await expect(page.locator('[data-testid="work-share-card"]')).toBeVisible()

    // 2. Reload / refresh page
    await page.reload()
    await expect(scoreboardTab).toHaveAttribute('data-state', 'active')
    await expect(page.locator('[data-testid="work-share-card"]')).toBeVisible()

    // 3. Switch to Zadania tab
    const choresTab = page.getByRole('tab', { name: 'Zadania' })
    await choresTab.click()
    await expect(choresTab).toHaveAttribute('data-state', 'active')
    expect(new URL(page.url()).pathname).toBe('/')

    // 4. Browser back button
    await page.goBack()
    await expect(scoreboardTab).toHaveAttribute('data-state', 'active')
    await expect(page.locator('[data-testid="work-share-card"]')).toBeVisible()
  })
})
