import { test as base } from '@playwright/test'
import { Household } from './household'
import { App } from '../drivers/app'
import { Backlog } from '../drivers/backlog'
import { Store } from '../drivers/store'
import { Scoreboard } from '../drivers/scoreboard'

export { expect } from '@playwright/test'
export type { Member, SeededCompletion } from './household'

export const test = base.extend<{
  household: Household
  app: App
  backlog: Backlog
  store: Store
  scoreboard: Scoreboard
}>({
  household: [
    async ({ request }, use) => {
      const household = await Household.open(request)
      try {
        await household.reset()
        await use(household)
      } finally {
        await household.close()
      }
    },
    { auto: true },
  ],
  app: async ({ page }, use) => use(new App(page)),
  backlog: async ({ page }, use) => use(new Backlog(page)),
  store: async ({ page }, use) => use(new Store(page)),
  scoreboard: async ({ page }, use) => use(new Scoreboard(page)),
})
