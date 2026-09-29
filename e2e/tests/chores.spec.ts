import { test, expect } from '@playwright/test'
import { resetTestUser, cleanupTestChores, seedOverdueChore } from '../helpers/db'

test.describe.serial('Chores Backlog UI Tests', () => {
  test.beforeEach(async () => {
    await resetTestUser()
    await cleanupTestChores('E2E_')
  })

  test.afterAll(async () => {
    await cleanupTestChores('E2E_')
  })

  test('renders chores tab with action bar and search input', async ({ page }) => {
    await page.goto('/')

    // Chores tab should be active by default
    const choresTab = page.getByRole('tab', { name: 'Zadania' })
    await expect(choresTab).toHaveAttribute('data-state', 'active')

    // Header title and add button should be visible
    await expect(page.getByRole('heading', { level: 2, name: /Zadania domowe/i })).toBeVisible()
    await expect(page.getByRole('button', { name: /Dodaj zadanie/i })).toBeVisible()
  })

  test('Golden Journey: creates new recurring chore "Mycie okien", completes with 1-tap, verifies points and freshness reset', async ({ page }) => {
    await page.goto('/')

    // Verify test mode and clean 0 pkt balance
    await expect(page.getByText('Test Mode')).toBeVisible()
    const initialCoins = page.locator('header').getByText(/0 pkt/)
    await expect(initialCoins).toBeVisible()

    // 1. Open Add Chore Modal
    const addButton = page.getByRole('button', { name: /Dodaj zadanie/i }).first()
    await addButton.click()

    // 2. Fill modal form for "Mycie okien" (cadence 30 days, tag #dom)
    const choreTitle = `E2E_Mycie okien_${Date.now()}`
    await page.locator('#chore-title').fill(choreTitle)
    await page.locator('#chore-desc').fill('Umyć szyby i parapety od wewnątrz i zewnątrz')

    // Select 'Co miesiąc' preset (30 days)
    await page.getByRole('button', { name: 'Co miesiąc' }).click()

    // Add tag 'dom'
    const tagInput = page.getByPlaceholder(/Nowy tag/i)
    await tagInput.fill('dom')
    await page.getByRole('button', { name: /Dodaj/i }).click()

    // Submit modal
    await page.getByRole('button', { name: /Utwórz zadanie/i }).click()

    // 3. Verify card appears in Scheduled section with green/freshness badge, cadence, and tag
    const choreCard = page.locator('[data-testid="chore-card"]', { hasText: choreTitle })

    await expect(choreCard).toBeVisible()
    await expect(choreCard.getByText('Świeże')).toBeVisible()
    await expect(choreCard.getByText('co 30 dni')).toBeVisible()
    await expect(choreCard.getByText('#dom')).toBeVisible()

    // 4. Tap "Zrobione!"
    const completeButton = choreCard.getByRole('button', { name: /Zrobione!/i })
    await completeButton.click()

    // 5. Verify optimistic UI feedback with points gained and disabled state
    const doneButton = choreCard.getByRole('button', { name: /Ukończono \(\+1 pkt\)/i })
    await expect(doneButton).toBeVisible()
    await expect(doneButton).toBeDisabled()
    await expect(doneButton).toHaveText(/\+1 pkt/)
    await expect(choreCard.getByText('Świeże')).toBeVisible()
    await expect(choreCard.getByText('zrobione dzisiaj')).toBeVisible()

    // 6. Verify member points increment in Header (0 pkt -> 1 pkt)
    const updatedCoins = page.locator('header').getByText(/1 pkt/)
    await expect(updatedCoins).toBeVisible()

    // 7. Dropdown menu on completed chore (can still edit/delete)
    const optionsButton = choreCard.getByRole('button', { name: /Więcej opcji/i })
    await optionsButton.click()
    await expect(page.getByRole('button', { name: /Edytuj/i })).toBeVisible()
    await expect(page.getByRole('button', { name: /Usuń/i })).toBeVisible()

    // 8. Status Transition Test: seed an overdue/neglected chore directly in wib_test
    const overdueTitle = `E2E_Piekarnik_${Date.now()}`
    await seedOverdueChore({ title: overdueTitle, cadenceDays: 14, daysAgo: 21 })

    // Reload page to reflect newly seeded chore
    await page.goto('/')

    // Verify overdue card appears with Red badge ('Zaniedbane')
    const overdueCard = page.locator('[data-testid="chore-card"]', { hasText: overdueTitle })
    await expect(overdueCard).toBeVisible()
    await expect(overdueCard.getByText('Zaniedbane', { exact: true })).toBeVisible()

    // Click "Zrobione!" on the overdue chore
    await overdueCard.getByRole('button', { name: /Zrobione!/i }).click()

    // Verify chore freshness resets to Green ('Świeże') and header points increment (1 pkt -> 2 pkt)
    await expect(overdueCard.getByRole('button', { name: /Ukończono \(\+1 pkt\)/i })).toBeVisible()
    await expect(overdueCard.getByText('Świeże', { exact: true })).toBeVisible()
    await expect(page.locator('header').getByText(/2 pkt/)).toBeVisible()
  })

  test('creates unscheduled chore and filters by search', async ({ page }) => {
    await page.goto('/')

    // Open Add Modal
    await page.getByRole('button', { name: /Dodaj zadanie/i }).first().click()

    const unscheduledTitle = `E2E_Naprawa kranu_${Date.now()}`
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

  test('creates chore quickly via condensed inline top form', async ({ page }) => {
    await page.goto('/')

    const quickChoreTitle = `E2E_Podlać kwiaty_${Date.now()}`
    const quickAddForm = page.locator('[data-testid="quick-add-chore"]')
    await expect(quickAddForm).toBeVisible()

    const input = quickAddForm.getByPlaceholder(/Dodaj nowe zadanie/i)
    await input.fill(quickChoreTitle)

    // Click 'Co tydzień' preset
    await quickAddForm.getByRole('button', { name: 'Co tydzień' }).click()

    // Click 'Dodaj' button
    await quickAddForm.getByRole('button', { name: 'Dodaj' }).click()

    // Chore card should appear in the scheduled list
    const choreCard = page.locator('[data-testid="chore-card"]', { hasText: quickChoreTitle })
    await expect(choreCard).toBeVisible()
    await expect(choreCard.getByText('Świeże')).toBeVisible()
  })

  test('flows all properties from quick add into modal when clicking Więcej', async ({ page }) => {
    await page.goto('/')

    const quickChoreTitle = `E2E_Odkurzanie salonu_${Date.now()}`
    const quickAddForm = page.locator('[data-testid="quick-add-chore"]')
    await expect(quickAddForm).toBeVisible()

    const input = quickAddForm.getByPlaceholder(/Dodaj nowe zadanie/i)
    await input.fill(quickChoreTitle)

    // Select 'Co 2 tyg.' in quick add
    await quickAddForm.getByRole('button', { name: 'Co 2 tyg.' }).click()

    // Click 'Więcej'
    await quickAddForm.getByRole('button', { name: /Więcej/i }).click()

    // Modal should be open with title pre-filled
    const modalTitleInput = page.locator('#chore-title')
    await expect(modalTitleInput).toHaveValue(quickChoreTitle)

    // The 'Co 2 tyg.' preset should be active in modal (amber active background)
    const activePreset = page.getByRole('dialog').getByRole('button', { name: 'Co 2 tyg.' })
    await expect(activePreset).toHaveClass(/bg-amber-500/)

    // Submit modal
    await page.getByRole('button', { name: /Utwórz zadanie/i }).click()

    // Verify chore appears in backlog with 14 days cadence
    const choreCard = page.locator('[data-testid="chore-card"]', { hasText: quickChoreTitle })
    await expect(choreCard).toBeVisible()
    await expect(choreCard.getByText('co 14 dni')).toBeVisible()

    // Verify quick add form is reset
    await expect(input).toHaveValue('')
    await expect(quickAddForm.getByRole('button', { name: 'Co tydzień' })).toHaveClass(/border-amber-300/)
  })

  test('edits an existing chore and deletes it with confirmation dialog', async ({ page }) => {
    await page.goto('/')

    // 1. Create a chore via modal
    const initialTitle = `E2E_DoEdycji_${Date.now()}`
    await page.getByRole('button', { name: /Dodaj zadanie/i }).first().click()
    await page.locator('#chore-title').fill(initialTitle)
    await page.getByRole('button', { name: 'Co tydzień' }).click()
    await page.getByRole('button', { name: /Utwórz zadanie/i }).click()

    const card = page.locator('[data-testid="chore-card"]', { hasText: initialTitle })
    await expect(card).toBeVisible()
    await expect(card.getByText('co 7 dni')).toBeVisible()

    // 2. Open dropdown menu -> click 'Edytuj'
    await card.getByRole('button', { name: /Więcej opcji/i }).click()
    await page.getByRole('button', { name: /Edytuj/i }).click()

    // 3. Modal opens with title pre-filled ('Edytuj zadanie')
    await expect(page.getByRole('heading', { name: /Edytuj zadanie/i })).toBeVisible()
    const titleInput = page.locator('#chore-title')
    await expect(titleInput).toHaveValue(initialTitle)

    // 4. Update title and cadence to 'Co 2 tyg.' (14 days)
    const updatedTitle = `E2E_Zmienione_${Date.now()}`
    await titleInput.fill(updatedTitle)
    await page.getByRole('button', { name: 'Co 2 tyg.' }).click()
    await page.getByRole('button', { name: /Zapisz zmiany/i }).click()

    // 5. Verify updated card appears with new title and cadence
    const updatedCard = page.locator('[data-testid="chore-card"]', { hasText: updatedTitle })
    await expect(updatedCard).toBeVisible()
    await expect(updatedCard.getByText('co 14 dni')).toBeVisible()
    await expect(page.locator('[data-testid="chore-card"]', { hasText: initialTitle })).toHaveCount(0)

    // 6. Open dropdown menu -> click 'Usuń'
    await updatedCard.getByRole('button', { name: /Więcej opcji/i }).click()
    await page.getByRole('button', { name: /Usuń/i }).click()

    // 7. Verify Delete Confirmation Dialog opens with prompt
    const deleteDialog = page.getByRole('dialog')
    await expect(deleteDialog.getByRole('heading', { name: /Usunąć zadanie\?/i })).toBeVisible()
    await expect(deleteDialog.getByText(updatedTitle)).toBeVisible()

    // 8. Confirm deletion
    await deleteDialog.getByRole('button', { name: 'Usuń' }).click()

    // 9. Verify card is removed from the backlog
    await expect(page.locator('[data-testid="chore-card"]', { hasText: updatedTitle })).toHaveCount(0)
  })

  test('prioritizes neglected and overdue chores at the top of the backlog', async ({ page }) => {
    // 1. Seed two chores directly in wib_test:
    // Chore A: Fresh chore (cadence 30 days, done today -> ratio 0.0)
    const freshTitle = `E2E_Swieze_${Date.now()}`
    await seedOverdueChore({ title: freshTitle, cadenceDays: 30, daysAgo: 0 })

    // Chore B: Neglected chore (cadence 14 days, done 28 days ago -> ratio 2.0 >= 1.30)
    const neglectedTitle = `E2E_Zaniedbane_${Date.now()}`
    await seedOverdueChore({ title: neglectedTitle, cadenceDays: 14, daysAgo: 28 })

    // 2. Load page
    await page.goto('/')

    // 3. Verify both cards appear
    const cards = page.locator('[data-testid="chore-card"]')
    await expect(cards).toHaveCount(2)

    // 4. Invariant: Neglected chore MUST be the first card in the list
    await expect(cards.first()).toContainText(neglectedTitle)
    await expect(cards.first().getByText('Zaniedbane', { exact: true })).toBeVisible()

    // 5. Fresh chore MUST be the second card in the list
    await expect(cards.nth(1)).toContainText(freshTitle)
    await expect(cards.nth(1).getByText('Świeże', { exact: true })).toBeVisible()
  })

  test('filters chores by clicking tag chips and clears filter', async ({ page }) => {
    await page.goto('/')

    // 1. Create Chore 1 with tag 'kuchnia'
    const titleKitchen = `E2E_Kuchnia_${Date.now()}`
    await page.getByRole('button', { name: /Dodaj zadanie/i }).first().click()
    await page.locator('#chore-title').fill(titleKitchen)
    const tagInput1 = page.getByPlaceholder(/Nowy tag/i)
    await tagInput1.fill('kuchnia')
    await page.getByRole('button', { name: /Dodaj/i }).click()
    await page.getByRole('button', { name: /Utwórz zadanie/i }).click()
    await expect(page.locator('[data-testid="chore-card"]', { hasText: titleKitchen })).toBeVisible()

    // 2. Create Chore 2 with tag 'ogrod'
    const titleGarden = `E2E_Ogrod_${Date.now()}`
    await page.getByRole('button', { name: /Dodaj zadanie/i }).first().click()
    await page.locator('#chore-title').fill(titleGarden)
    const tagInput2 = page.getByPlaceholder(/Nowy tag/i)
    await tagInput2.fill('ogrod')
    await page.getByRole('button', { name: /Dodaj/i }).click()
    await page.getByRole('button', { name: /Utwórz zadanie/i }).click()
    await expect(page.locator('[data-testid="chore-card"]', { hasText: titleGarden })).toBeVisible()

    // 3. Verify tag pills appear in filter bar
    const filterBar = page.locator('[data-testid="chore-filters"]')
    const kitchenTagBtn = filterBar.getByRole('button', { name: '#kuchnia' })
    const gardenTagBtn = filterBar.getByRole('button', { name: '#ogrod' })
    await expect(kitchenTagBtn).toBeVisible()
    await expect(gardenTagBtn).toBeVisible()

    // 4. Click '#kuchnia' filter
    await kitchenTagBtn.click()
    await expect(page.locator('[data-testid="chore-card"]', { hasText: titleKitchen })).toBeVisible()
    await expect(page.locator('[data-testid="chore-card"]', { hasText: titleGarden })).toHaveCount(0)

    // 5. Click '#ogrod' filter (multi-tag OR)
    await gardenTagBtn.click()
    await expect(page.locator('[data-testid="chore-card"]', { hasText: titleKitchen })).toBeVisible()
    await expect(page.locator('[data-testid="chore-card"]', { hasText: titleGarden })).toBeVisible()

    // 6. Click '#kuchnia' again to toggle it off -> only garden chore matches
    await kitchenTagBtn.click()
    await expect(page.locator('[data-testid="chore-card"]', { hasText: titleKitchen })).toHaveCount(0)
    await expect(page.locator('[data-testid="chore-card"]', { hasText: titleGarden })).toBeVisible()

    // 7. Click 'Wszystkie' -> reset filter, both chores visible
    await filterBar.getByRole('button', { name: 'Wszystkie' }).click()
    await expect(page.locator('[data-testid="chore-card"]', { hasText: titleKitchen })).toBeVisible()
    await expect(page.locator('[data-testid="chore-card"]', { hasText: titleGarden })).toBeVisible()
  })
})
