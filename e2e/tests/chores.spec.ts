import { test, expect } from '../fixtures'

test.describe('Chores Backlog UI Tests', () => {
  test('renders chores tab with action bar and search input', async ({ app, backlog }) => {
    await app.goto()

    await expect(app.tab('Zadania')).toHaveAttribute('data-state', 'active')
    await expect(backlog.heading).toBeVisible()
    await expect(backlog.addButton).toBeVisible()
  })

  test('Golden Journey: creates new recurring chore "Mycie okien", completes with 1-tap, verifies points and freshness reset', async ({ app, backlog, household, page }) => {
    await app.goto()
    await expect(page.getByText('Test Mode')).toBeVisible()
    await expect(app.points(0)).toBeVisible()

    const chore = await backlog.addChore({
      title: 'Mycie okien',
      description: 'Umyć szyby i parapety od wewnątrz i zewnątrz',
      cadence: 'Co miesiąc',
      tags: ['dom'],
    })
    await expect(chore.text('Świeże')).toBeVisible()
    await expect(chore.text('co 30 dni')).toBeVisible()
    await expect(chore.text('dom')).toBeVisible()

    await chore.complete()
    await expect(chore.doneButton).toBeDisabled()
    await expect(chore.doneButton).toHaveText(/\+1 pkt/)
    await expect(chore.text('Świeże')).toBeVisible()
    await expect(chore.text('zrobione dzisiaj')).toBeVisible()
    await expect(app.points(1)).toBeVisible()

    await chore.toggleMenu()
    await expect(chore.menuItem('Edytuj')).toBeVisible()
    await expect(chore.menuItem('Usuń')).toBeVisible()

    await household.chore({ title: 'Piekarnik', cadenceDays: 14, lastDoneDaysAgo: 21 })
    await app.goto()

    const overdue = backlog.chore('Piekarnik')
    await expect(overdue.text('Zaniedbane')).toBeVisible()

    await overdue.complete()
    await expect(overdue.doneButton).toBeVisible()
    await expect(overdue.text('Świeże')).toBeVisible()
    await expect(app.points(2)).toBeVisible()
  })

  test('unscheduled chore shows real last-done day and stays locked after completion and reload', async ({ app, backlog, household, page }) => {
    await household.chore({ title: 'Śmieci', cadenceDays: null, lastDoneDaysAgo: 3 })

    await app.goto()
    const chore = backlog.chore('Śmieci')
    await expect(chore.text('zrobione 3 dni temu')).toBeVisible()

    await chore.complete()
    await expect(chore.doneButton).toBeDisabled()

    await page.reload()
    await expect(chore.doneButton).toBeDisabled()
    await expect(chore.text('zrobione dzisiaj')).toBeVisible()
  })

  test('same-day repeat: locked chore can be recorded again from the menu after confirmation', async ({ app, backlog, household }) => {
    await household.chore({ title: 'Odkurzanie', cadenceDays: 7, lastDoneDaysAgo: 3 })

    await app.goto()
    const chore = backlog.chore('Odkurzanie')
    await expect(chore.root).toBeVisible()

    await chore.toggleMenu()
    await expect(chore.menuItem('Zrobione ponownie')).toHaveCount(0)
    await chore.toggleMenu()

    await chore.complete()
    await expect(chore.doneButton).toBeDisabled()
    await expect(app.points(1)).toBeVisible()

    const dialog = await chore.openRepeat()
    await expect(dialog.root).toContainText('Odkurzanie')
    await expect(dialog.root).toContainText('+1 pkt')
    await dialog.cancel()
    await expect(app.points(1)).toBeVisible()

    await (await chore.openRepeat()).confirm()
    await expect(app.points(2)).toBeVisible()

    await expect(chore.doneButton).toBeDisabled()
    await chore.toggleMenu()
    await expect(chore.menuItem('Zrobione ponownie')).toBeVisible()
  })

  test('creates unscheduled chore and filters by search', async ({ app, backlog, page }) => {
    await app.goto()

    const chore = await backlog.addChore({ title: 'Naprawa kranu', cadence: 'Bez terminu' })
    await expect(chore.text('Bez terminu')).toBeVisible()

    await backlog.cadenceFilter('Bez terminu').click()
    await expect(page).toHaveURL(/cadence=none/)
    await expect(chore.root).toBeVisible()

    await backlog.search('Naprawa kranu')
    await expect(chore.root).toBeVisible()

    await chore.complete()
    await expect(chore.doneButton).toBeVisible()
    await expect(chore.text('Bez terminu')).toBeVisible()
    await expect(chore.text('Świeże')).toHaveCount(0)

    await backlog.search('Nieistniejące zadanie 12345')
    await expect(backlog.noSearchResults).toBeVisible()
  })

  test('is responsive on mobile viewport', async ({ app, page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
    await app.goto()

    await expect(app.header).toBeVisible()
    await expect(app.bottomNav).toBeVisible()
    await expect(app.bottomNav.getByRole('tab', { name: 'Zadania' })).toBeVisible()
  })

  test('edits an existing chore and deletes it with confirmation dialog', async ({ app, backlog }) => {
    await app.goto()

    const original = await backlog.addChore({ title: 'Do edycji', cadence: 'Co tydzień' })
    await expect(original.text('co 7 dni')).toBeVisible()

    const form = await original.edit()
    await expect(form.heading).toHaveText(/Edytuj zadanie/i)
    await expect(form.titleInput).toHaveValue('Do edycji')
    await form.fill({ title: 'Zmienione', cadence: 'Co 3 mies.' })
    await form.submit()

    const updated = backlog.chore('Zmienione')
    await expect(updated.text('co 90 dni')).toBeVisible()
    await expect(original.root).toHaveCount(0)

    const deleteDialog = await updated.openDelete()
    await expect(deleteDialog.getByRole('heading', { name: /Usunąć zadanie\?/i })).toBeVisible()
    await expect(deleteDialog.getByText('Zmienione')).toBeVisible()
    await deleteDialog.getByRole('button', { name: 'Usuń' }).click()

    await expect(updated.root).toHaveCount(0)
  })

  test('prioritizes neglected and overdue chores at the top of the backlog', async ({ app, backlog, household }) => {
    await household.chore({ title: 'Lodówka', cadenceDays: 30, lastDoneDaysAgo: 0 })
    await household.chore({ title: 'Fugi w łazience', cadenceDays: 14, lastDoneDaysAgo: 28 })

    await app.goto()

    await expect(backlog.cards).toHaveCount(2)
    await expect(backlog.cards.first()).toContainText('Fugi w łazience')
    await expect(backlog.cards.first().getByText('Zaniedbane', { exact: true })).toBeVisible()
    await expect(backlog.cards.nth(1)).toContainText('Lodówka')
    await expect(backlog.cards.nth(1).getByText('Świeże', { exact: true })).toBeVisible()
  })

  test('filters chores by clicking tag chips and clears filter', async ({ app, backlog }) => {
    await app.goto()

    const kitchen = await backlog.addChore({ title: 'Kuchnia', tags: ['kuchnia'] })
    const garden = await backlog.addChore({ title: 'Ogród', tags: ['ogrod'] })

    await expect(backlog.tagFilter('kuchnia')).toBeVisible()
    await expect(backlog.tagFilter('ogrod')).toBeVisible()

    await backlog.tagFilter('kuchnia').click()
    await expect(kitchen.root).toBeVisible()
    await expect(garden.root).toHaveCount(0)

    await backlog.tagFilter('ogrod').click()
    await expect(kitchen.root).toBeVisible()
    await expect(garden.root).toBeVisible()

    await backlog.tagFilter('kuchnia').click()
    await expect(kitchen.root).toHaveCount(0)
    await expect(garden.root).toBeVisible()

    await backlog.allTagsFilter.click()
    await expect(kitchen.root).toBeVisible()
    await expect(garden.root).toBeVisible()
  })

  test('filters by cadence range and persists all filters in the URL across reload', async ({ app, backlog, household, page }) => {
    await household.chore({ title: 'Pranie zasłon', cadenceDays: 14 })
    await household.chore({ title: 'Podlewanie kwiatów', cadenceDays: 7 })
    await household.chore({ title: 'Wymiana żarówki', cadenceDays: null })

    await app.goto()
    await expect(backlog.cards).toHaveCount(3)
    await expect(backlog.cadenceFilter('Codziennie')).toHaveCount(0)

    const monthly = backlog.cadenceFilter('Co miesiąc')
    await monthly.click()
    await expect(backlog.cards).toHaveCount(1)
    await expect(backlog.cards.first()).toContainText('Pranie zasłon')

    await backlog.cadenceFilter('Co tydzień').click()
    await expect(backlog.cards).toHaveCount(1)
    await expect(backlog.cards.first()).toContainText('Podlewanie kwiatów')
    await expect(monthly).toHaveAttribute('aria-pressed', 'false')

    await backlog.cadenceFilter('Co tydzień').click()
    await expect(backlog.cards).toHaveCount(3)

    await monthly.click()
    await backlog.search('zasłon')
    await expect(page).toHaveURL(/q=zas/)
    await expect(page).toHaveURL(/cadence=30/)

    await page.reload()
    await expect(backlog.searchInput).toHaveValue('zasłon')
    await expect(monthly).toHaveAttribute('aria-pressed', 'true')
    await expect(backlog.cards).toHaveCount(1)
    await expect(backlog.cards.first()).toContainText('Pranie zasłon')

    await backlog.search('Nieistniejące zadanie 12345')
    await backlog.clearFiltersButton(2).click()
    await expect(backlog.cards).toHaveCount(3)
    await expect(page).not.toHaveURL(/\?/)
  })

  test('mobile viewport: Add Chore modal is scrollable and submit button is visible and clickable on small mobile screen', async ({ app, backlog, page }) => {
    await page.setViewportSize({ width: 375, height: 600 })
    await app.goto()

    const form = await backlog.openNewChoreForm()
    await expect(form.dialog).toBeVisible()
    await expect(form.submitButton).toBeVisible()

    await form.fill({ title: 'Mobilne' })
    await expect(form.dialog.locator('.overflow-y-auto')).toBeVisible()

    await form.submit()
    await expect(backlog.chore('Mobilne').root).toBeVisible()
  })
})
