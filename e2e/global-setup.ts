import mysql from 'mysql2/promise'
import { getDbConfig } from './helpers/db'

export default async function globalSetup() {
  const config = getDbConfig()
  const conn = await mysql.createConnection({
    host: config.host,
    port: config.port,
    user: config.user,
    password: config.password,
  })
  try {
    await conn.query(`CREATE DATABASE IF NOT EXISTS \`${config.database}\``)
  } finally {
    await conn.end()
  }
}
