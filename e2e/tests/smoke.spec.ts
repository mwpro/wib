import { test, expect } from '@playwright/test'

test.describe('Walking Skeleton E2E Smoke Tests', () => {
  test('backend health endpoint responds with Healthy', async ({ request }) => {
    const response = await request.get('/api/health')
    expect(response.ok()).toBeTruthy()
    const text = await response.text()
    expect(text).toBe('Healthy')
  })

  test('backend config endpoint returns valid configuration', async ({ request }) => {
    const response = await request.get('/api/config')
    expect(response.ok()).toBeTruthy()
    const config = await response.json()
    expect(config).toHaveProperty('jwtAuth')
    expect(config).toHaveProperty('isTestMode')
  })

  test('app launches and renders navigation tabs with test auth bypass', async ({ page }) => {
    await page.goto('/')

    // App header & title
    const header = page.locator('header')
    await expect(header).toBeVisible()
    await expect(header.getByRole('heading', { level: 1 })).toHaveText('wib')

    // In test auth bypass mode, the Test Mode badge should be displayed
    await expect(page.getByText('Test Mode')).toBeVisible()

    // Main navigation tabs in Polish
    const choresTab = page.getByRole('tab', { name: 'Zadania' })
    const scoreboardTab = page.getByRole('tab', { name: 'Kto jest lepszy?' })
    const storeTab = page.getByRole('tab', { name: 'Sklep' })

    await expect(choresTab).toBeVisible()
    await expect(scoreboardTab).toBeVisible()
    await expect(storeTab).toBeVisible()

    // Tab interaction
    await scoreboardTab.click()
    await expect(scoreboardTab).toHaveAttribute('data-state', 'active')

    await storeTab.click()
    await expect(storeTab).toHaveAttribute('data-state', 'active')

    await choresTab.click()
    await expect(choresTab).toHaveAttribute('data-state', 'active')
  })
})
