import mysql from 'mysql2/promise'

function parseConnectionString(connStr?: string) {
  if (!connStr) return null
  const parts = Object.fromEntries(
    connStr
      .split(';')
      .filter(Boolean)
      .map((kv) => {
        const [k, ...v] = kv.split('=')
        return [k.trim().toLowerCase(), v.join('=').trim()]
      })
  )
  return {
    host: parts['server'] || 'localhost',
    port: Number(parts['port'] || 3306),
    user: parts['user'] || 'root',
    password: parts['password'] || 'secret',
    database: parts['database'] || 'wib_test',
  }
}

export function getDbConfig() {
  return (
    parseConnectionString(process.env.ConnectionStrings__DefaultConnection) || {
      host: process.env.DB_HOST || 'localhost',
      port: Number(process.env.DB_PORT || 3306),
      user: process.env.DB_USER || 'root',
      password: process.env.DB_PASSWORD || 'secret',
      database: process.env.DB_NAME || 'wib_test',
    }
  )
}

export async function resetTestUser(externalSubjectId = 'auth0|test-user-1') {
  const config = getDbConfig()
  const conn = await mysql.createConnection(config)
  try {
    const [rows] = await conn.query<any[]>(
      'SELECT Id FROM members WHERE ExternalSubjectId = ?',
      [externalSubjectId]
    )
    if (rows && rows.length > 0) {
      const memberId = rows[0].Id
      await conn.query('DELETE FROM vouchers WHERE OwnedByMemberId = ?', [memberId])
      await conn.query('DELETE FROM chore_completions WHERE CompletedByMemberId = ?', [memberId])
      await conn.query('UPDATE members SET WalletBalance = 0 WHERE Id = ?', [memberId])
    }
  } finally {
    await conn.end()
  }
}

export async function setWalletBalance(externalSubjectId: string, balance: number) {
  const config = getDbConfig()
  const conn = await mysql.createConnection(config)
  try {
    await conn.query(
      'UPDATE members SET WalletBalance = ? WHERE ExternalSubjectId = ?',
      [balance, externalSubjectId]
    )
  } finally {
    await conn.end()
  }
}

export async function seedOverdueChore({
  title,
  cadenceDays = 14,
  daysAgo = 21,
}: {
  title: string
  cadenceDays?: number
  daysAgo?: number
}) {
  const config = getDbConfig()
  const conn = await mysql.createConnection(config)
  try {
    const pastDate = new Date(Date.now() - daysAgo * 24 * 60 * 60 * 1000)
    const formattedDate = pastDate.toISOString().slice(0, 19).replace('T', ' ')
    const [result] = await conn.query<any>(
      `INSERT INTO chores (Title, Description, Points, CadenceDays, LastCompletedAt, IsArchived, CreatedAt)
       VALUES (?, ?, 1, ?, ?, 0, ?)`,
      [title, 'Zadanie wygenerowane do testu zaległości', cadenceDays, formattedDate, formattedDate]
    )
    return { id: result.insertId, title }
  } finally {
    await conn.end()
  }
}

export async function cleanupTestChores(prefix = 'E2E_') {
  const config = getDbConfig()
  const conn = await mysql.createConnection(config)
  try {
    const pattern = `%${prefix}%`
    await conn.query(
      `DELETE ct FROM chore_tags ct
       INNER JOIN chores c ON ct.ChoreId = c.Id
       WHERE c.Title LIKE ?`,
      [pattern]
    )
    await conn.query(
      `DELETE cc FROM chore_completions cc
       INNER JOIN chores c ON cc.ChoreId = c.Id
       WHERE c.Title LIKE ?`,
      [pattern]
    )
    await conn.query('DELETE FROM chores WHERE Title LIKE ?', [pattern])
  } finally {
    await conn.end()
  }
}

export async function cleanupTestRewards(prefix = 'TEST_REWARD_') {
  const config = getDbConfig()
  const conn = await mysql.createConnection(config)
  try {
    const pattern = `%${prefix}%`
    await conn.query(
      `DELETE v FROM vouchers v
       INNER JOIN reward_items r ON v.RewardItemId = r.Id
       WHERE r.Title LIKE ?`,
      [pattern]
    )
    await conn.query('DELETE FROM reward_items WHERE Title LIKE ?', [pattern])
  } finally {
    await conn.end()
  }
}

