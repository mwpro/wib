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
