import { test, expect } from '../fixtures'

test.describe('Walking Skeleton E2E Smoke Tests', () => {
  test('backend health endpoint responds with Healthy', async ({ request }) => {
    const response = await request.get('/api/health')
    expect(response.ok()).toBeTruthy()
    expect(await response.text()).toBe('Healthy')
  })

  test('backend config endpoint returns valid configuration', async ({ request }) => {
    const response = await request.get('/api/config')
    expect(response.ok()).toBeTruthy()
    const config = await response.json()
    expect(config).toHaveProperty('jwtAuth')
    expect(config).toHaveProperty('isTestMode')
  })

  test('app launches and renders navigation tabs with test auth bypass', async ({ app, page }) => {
    await app.goto()

    await expect(app.header).toBeVisible()
    await expect(app.header.getByRole('heading', { level: 1 })).toHaveText('wib')
    await expect(page.getByText('Test Mode')).toBeVisible()

    for (const name of ['Kto jest lepszy?', 'Sklep', 'Zadania'] as const) {
      await app.openTab(name)
      await expect(app.tab(name)).toHaveAttribute('data-state', 'active')
    }
  })
})
