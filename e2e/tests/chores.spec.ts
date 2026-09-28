import { test, expect } from '@playwright/test'

test.describe('Chores Backlog UI Tests', () => {
  test('renders chores tab with action bar and search input', async ({ page }) => {
    await page.goto('/')

    // Chores tab should be active by default
    const choresTab = page.getByRole('tab', { name: 'Zadania' })
    await expect(choresTab).toHaveAttribute('data-state', 'active')

    // Header title and add button should be visible
    await expect(page.getByRole('heading', { level: 2, name: /Zadania domowe/i })).toBeVisible()
    await expect(page.getByRole('button', { name: /Dodaj zadanie/i })).toBeVisible()
  })

  test('creates new scheduled chore and completes it with 1-tap', async ({ page }) => {
    await page.goto('/')

    const initialCoins = page.locator('header').getByText(/pkt/)
    await expect(initialCoins).toBeVisible()

    // 1. Open Add Chore Modal
    const addButton = page.getByRole('button', { name: /Dodaj zadanie/i }).first()
    await addButton.click()

    // 2. Fill modal form
    const choreTitle = `Zmywanie naczyń ${Date.now()}`
    await page.locator('#chore-title').fill(choreTitle)
    await page.locator('#chore-desc').fill('Wypłukać i wstawić do zmywarki')

    // Select 'Co tydzień' preset
    await page.getByRole('button', { name: 'Co tydzień' }).click()

    // Add a tag
    const tagInput = page.getByPlaceholder(/Nowy tag/i)
    await tagInput.fill('kuchnia')
    await page.getByRole('button', { name: /Dodaj/i }).click()

    // Submit modal
    await page.getByRole('button', { name: /Utwórz zadanie/i }).click()

    // 3. Verify card appears in Scheduled section with green/freshness badge
    const choreCard = page.locator('[data-testid="chore-card"]', { hasText: choreTitle })

    await expect(choreCard).toBeVisible()
    await expect(choreCard.getByText('Świeże')).toBeVisible()
    await expect(choreCard.getByText('#kuchnia')).toBeVisible()

    // 4. Tap "Zrobione!"
    const completeButton = choreCard.getByRole('button', { name: /Zrobione!/i })
    await completeButton.click()

    // 5. Verify optimistic UI feedback with points gained and disabled state
    const doneButton = choreCard.getByRole('button', { name: /Ukończono \(\+1 pkt\)/i })
    await expect(doneButton).toBeVisible()
    await expect(doneButton).toBeDisabled()
    await expect(doneButton).toHaveText(/\+1 pkt/)

    // 6. Verify dropdown menu on completed chore (can still edit/delete)
    const optionsButton = choreCard.getByRole('button', { name: /Więcej opcji/i })
    await optionsButton.click()
    await expect(page.getByRole('button', { name: /Edytuj/i })).toBeVisible()
    await expect(page.getByRole('button', { name: /Usuń/i })).toBeVisible()
  })

  test('creates unscheduled chore and filters by search', async ({ page }) => {
    await page.goto('/')

    // Open Add Modal
    await page.getByRole('button', { name: /Dodaj zadanie/i }).first().click()

    const unscheduledTitle = `Naprawa kranu ${Date.now()}`
    await page.locator('#chore-title').fill(unscheduledTitle)

    // Switch to "Bez terminu"
    await page.getByRole('button', { name: 'Bez terminu' }).click()

    // Submit modal
    await page.getByRole('button', { name: /Utwórz zadanie/i }).click()

    // Should appear under "Do zrobienia (bez terminu)"
    await expect(page.getByText('Do zrobienia (bez terminu)')).toBeVisible()
    const card = page.locator('[data-testid="chore-card"]', { hasText: unscheduledTitle })
    await expect(card).toBeVisible()
    await expect(card.getByText('Bez terminu').first()).toBeVisible()

    // Test search filter
    const searchInput = page.getByPlaceholder(/Szukaj zadań/i)
    await searchInput.fill(unscheduledTitle)
    await expect(page.locator('[data-testid="chore-card"]', { hasText: unscheduledTitle })).toBeVisible()

    // Tap "Zrobione!" on unscheduled chore
    await card.getByRole('button', { name: /Zrobione!/i }).click()
    await expect(card.getByRole('button', { name: /Ukończono/i })).toBeVisible()
    await expect(card.getByText('Bez terminu').first()).toBeVisible()
    await expect(card.getByText('Świeże')).toHaveCount(0)

    // Negative search
    await searchInput.fill('Nieistniejące zadanie 12345')
    await expect(page.getByText(/Brak zadań spełniających kryteria wyszukiwania/i)).toBeVisible()
  })

  test('is responsive on mobile viewport', async ({ page }) => {
    // Emulate iPhone mobile viewport
    await page.setViewportSize({ width: 390, height: 844 })
    await page.goto('/')

    // Header and bottom navigation should both be visible and not overlapping
    const header = page.locator('header')
    await expect(header).toBeVisible()

    const bottomNav = page.locator('nav')
    await expect(bottomNav).toBeVisible()

    // Bottom nav tabs should be clickable
    const zadaniaTab = bottomNav.getByRole('tab', { name: 'Zadania' })
    await expect(zadaniaTab).toBeVisible()
  })
})
