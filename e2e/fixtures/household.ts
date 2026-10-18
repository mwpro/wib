import mysql, { type Connection } from 'mysql2/promise'
import type { BrowserContext, Page } from '@playwright/test'
import { getDbConfig } from './db-config'

export type Urgency = 'Fresh' | 'DueSoon' | 'Overdue' | 'Neglected' | 'Unscheduled'

export interface Member {
  id: number
  sub: string
  name: string
  actAs(target: Page | BrowserContext): Promise<void>
}

export interface SeededCompletion {
  by: Member
  daysSincePrevious: number
  urgencyAtCompletion: Urgency
}

export interface SeededChore {
  id: number
  title: string
}

const DAY_MS = 24 * 60 * 60 * 1000

const toSql = (d: Date) => d.toISOString().slice(0, 19).replace('T', ' ')

function warsawDaysAgo(days: number) {
  const [year, month, day] = new Intl.DateTimeFormat('en-CA', { timeZone: 'Europe/Warsaw' })
    .format(new Date())
    .split('-')
    .map(Number)
  return new Date(Date.UTC(year, month - 1, day - days, 10))
}

export class Household {
  private memberCount = 0

  private constructor(private readonly conn: Connection) {}

  static async open() {
    const config = getDbConfig()
    if (!config.database.endsWith('_test')) {
      throw new Error(`Refusing to run e2e against "${config.database}": database name must end with _test`)
    }
    return new Household(await mysql.createConnection(config))
  }

  async close() {
    await this.conn.end()
  }

  async reset() {
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
      'INSERT INTO members (ExternalSubjectId, Name, WalletBalance, CreatedAt) VALUES (?, ?, ?, ?)',
      [sub, name, wallet, toSql(new Date())]
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
    lastDoneDaysAgo = 0,
  }: {
    title: string
    cadenceDays?: number | null
    points?: number
    lastDoneDaysAgo?: number
  }): Promise<SeededChore> {
    const doneAt = toSql(warsawDaysAgo(lastDoneDaysAgo))
    const [result] = await this.conn.query<mysql.ResultSetHeader>(
      `INSERT INTO chores (Title, Description, Points, CadenceDays, LastCompletedAt, IsArchived, CreatedAt)
       VALUES (?, NULL, ?, ?, ?, 0, ?)`,
      [title, points, cadenceDays, doneAt, doneAt]
    )
    return { id: result.insertId, title }
  }

  async choreWithHistory({
    title,
    cadenceDays,
    points = 2,
    createdDaysAgo,
    completions,
  }: {
    title: string
    cadenceDays: number | null
    points?: number
    createdDaysAgo: number
    completions: SeededCompletion[]
  }): Promise<SeededChore> {
    const createdAt = warsawDaysAgo(createdDaysAgo)
    let at = createdAt
    const rows = completions.map((c) => {
      at = new Date(at.getTime() + c.daysSincePrevious * DAY_MS)
      return { ...c, completedAt: toSql(at) }
    })

    const [result] = await this.conn.query<mysql.ResultSetHeader>(
      `INSERT INTO chores (Title, Description, Points, CadenceDays, LastCompletedAt, IsArchived, CreatedAt)
       VALUES (?, NULL, ?, ?, ?, 0, ?)`,
      [title, points, cadenceDays, rows.length > 0 ? toSql(at) : null, toSql(createdAt)]
    )
    const choreId = result.insertId

    for (const row of rows) {
      await this.conn.query(
        `INSERT INTO chore_completions
           (ChoreId, CompletedByMemberId, CompletedAt, PointsAwarded, DaysSincePrevious, UrgencyAtCompletion)
         VALUES (?, ?, ?, ?, ?, ?)`,
        [choreId, row.by.id, row.completedAt, points, row.daysSincePrevious, row.urgencyAtCompletion]
      )
    }

    return { id: choreId, title }
  }
}
