import { test, expect } from '@playwright/test'
import { resetTestUser, cleanupTestChores } from '../helpers/db'

const SB_USER = {
  sub: 'auth0|test-scoreboard-user',
  name: 'Scoreboard Tester',
  email: 'sb@test.com',
}

test.describe.serial('Scoreboard & Activity Stream UI Tests', () => {
  test.beforeEach(async ({ page }) => {
    await resetTestUser(SB_USER.sub)
    await cleanupTestChores('E2E_SB_')
    await page.addInitScript((user) => {
      localStorage.setItem('wib_test_user', JSON.stringify(user))
    }, SB_USER)
  })

  test.afterAll(async () => {
    await cleanupTestChores('E2E_SB_')
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

  test('updates work share, monthly podium, and activity stream when completing a chore', async ({ page }) => {
    await page.goto('/')

    // 1. Create a chore
    const choreTitle = `E2E_SB_Chore_${Date.now()}`
    const quickAddForm = page.locator('[data-testid="quick-add-chore"]')
    await quickAddForm.getByPlaceholder(/Dodaj nowe zadanie/i).fill(choreTitle)
    await quickAddForm.getByRole('button', { name: 'Dodaj' }).click()

    const choreCard = page.locator('[data-testid="chore-card"]', { hasText: choreTitle })
    await expect(choreCard).toBeVisible()

    // 2. Complete the chore
    await choreCard.getByRole('button', { name: /Zrobione!/i }).click()
    await expect(choreCard.getByRole('button', { name: /Ukończono/i })).toBeVisible()

    // 3. Switch to scoreboard tab
    await page.getByRole('tab', { name: /Kto jest lepszy\?/i }).click()

    // 4. Verify Work Share reflects points
    const workShareCard = page.locator('[data-testid="work-share-card"]')
    await expect(workShareCard).toBeVisible()
    await expect(workShareCard).toContainText(/Łącznie: [1-9]\d* pkt/)

    // 5. Verify Monthly Podium includes completion
    const podium = page.locator('[data-testid="monthly-podium"]')
    await expect(podium).toBeVisible()
    await expect(podium).toContainText('🥇')
    await expect(podium).toContainText(/1 zadanie/i)

    // 6. Verify Activity Stream has the entry
    const activityStream = page.locator('[data-testid="activity-stream"]')
    await expect(activityStream).toBeVisible()
    await expect(activityStream).toContainText(choreTitle)
    await expect(activityStream).toContainText('ukończył(a):')
    await expect(activityStream).toContainText('+1 pkt')

    // 7. Verify Lifetime Stats
    const lifetimeStats = page.locator('[data-testid="lifetime-stats"]')
    await expect(lifetimeStats).toBeVisible()
    await expect(lifetimeStats).toContainText(/Łącznie pkt/i)
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
