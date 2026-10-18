import mysql, { type Connection } from 'mysql2/promise'
import { expect, type APIRequestContext, type BrowserContext, type Page } from '@playwright/test'
import { getDbConfig } from './db-config'

export interface Member {
  id: number
  sub: string
  name: string
  actAs(target: Page | BrowserContext): Promise<void>
}

export interface SeededCompletion {
  by: Member
  daysSincePrevious: number
}

export interface SeededChore {
  id: number
  title: string
}

const DAY_MS = 24 * 60 * 60 * 1000

const DEFAULT_IDENTITY = { sub: 'auth0|test-user-1', name: 'Test Użytkownik' }

function warsawDaysAgo(days: number) {
  const [year, month, day] = new Intl.DateTimeFormat('en-CA', { timeZone: 'Europe/Warsaw' })
    .format(new Date())
    .split('-')
    .map(Number)
  return new Date(Date.UTC(year, month - 1, day - days, 10))
}

function identityHeaders({ sub, name }: { sub: string; name: string }) {
  return { 'X-Test-Sub': encodeURIComponent(sub), 'X-Test-User-Name': encodeURIComponent(name) }
}

export class Household {
  private memberCount = 0

  private constructor(
    private readonly conn: Connection,
    private readonly api: APIRequestContext
  ) {}

  static async open(api: APIRequestContext) {
    const config = getDbConfig()
    if (!config.database.endsWith('_test')) {
      throw new Error(`Refusing to run e2e against "${config.database}": database name must end with _test`)
    }
    return new Household(await mysql.createConnection(config), api)
  }

  async close() {
    await this.conn.end()
  }

  async reset() {
    await this.resetClock()
    const [tables] = await this.conn.query<mysql.RowDataPacket[]>(
      `SELECT TABLE_NAME AS name FROM information_schema.TABLES
       WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME <> '__EFMigrationsHistory'`
    )
    await this.conn.query('SET FOREIGN_KEY_CHECKS = 0')
    try {
      for (const { name } of tables) {
        await this.conn.query(`TRUNCATE TABLE \`${name}\``)
      }
    } finally {
      await this.conn.query('SET FOREIGN_KEY_CHECKS = 1')
    }
  }

  async member({ name, wallet = 0 }: { name: string; wallet?: number }): Promise<Member> {
    const sub = `auth0|e2e-member-${++this.memberCount}`
    const [result] = await this.conn.query<mysql.ResultSetHeader>(
      'INSERT INTO members (ExternalSubjectId, Name, WalletBalance, CreatedAt) VALUES (?, ?, ?, UTC_TIMESTAMP())',
      [sub, name, wallet]
    )
    return {
      id: result.insertId,
      sub,
      name,
      actAs: async (target) => {
        await target.addInitScript((user) => {
          localStorage.setItem('wib_test_user', JSON.stringify(user))
        }, { sub, name })
      },
    }
  }

  async chore({
    title,
    cadenceDays = null,
    points = 1,
    createdDaysAgo = 0,
    completions = [],
  }: {
    title: string
    cadenceDays?: number | null
    points?: number
    createdDaysAgo?: number
    completions?: SeededCompletion[]
  }): Promise<SeededChore> {
    let at = warsawDaysAgo(createdDaysAgo)
    try {
      if (createdDaysAgo > 0) await this.setClock(at)
      const created = await this.api.post('/api/chores', {
        headers: identityHeaders(DEFAULT_IDENTITY),
        data: { title, points, cadenceDays },
      })
      expect(created.status(), await created.text()).toBe(201)
      const { id } = await created.json()

      for (const completion of completions) {
        at = new Date(at.getTime() + completion.daysSincePrevious * DAY_MS)
        await this.setClock(at)
        const completed = await this.api.post(`/api/chores/${id}/completion`, {
          headers: identityHeaders(completion.by),
        })
        expect(completed.status(), await completed.text()).toBe(200)
      }

      return { id, title }
    } finally {
      await this.resetClock()
    }
  }

  private async setClock(utcNow: Date) {
    const response = await this.api.put('/api/test/clock', { data: { utcNow: utcNow.toISOString() } })
    expect(response.status(), 'test clock requires ASPNETCORE_ENVIRONMENT=Development').toBe(204)
  }

  private async resetClock() {
    const response = await this.api.delete('/api/test/clock')
    expect(response.status(), 'test clock requires ASPNETCORE_ENVIRONMENT=Development').toBe(204)
  }
}
