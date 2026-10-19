import type { AuthContextValue } from '../auth/context'
import { domainErrorMessage } from './errors'

type QueryValue = string | number | boolean | undefined

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  query?: Record<string, QueryValue>
  body?: unknown
  failureMessage: string
}

export class ApiError extends Error {
  readonly status: number
  readonly code?: string

  constructor(message: string, status: number, code?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

function buildUrl(path: string, query?: Record<string, QueryValue>): string {
  const params = new URLSearchParams()
  Object.entries(query ?? {}).forEach(([key, value]) => {
    if (value !== undefined) params.set(key, String(value))
  })
  const search = params.toString()
  return `/api${path}${search ? `?${search}` : ''}`
}

async function readErrorCode(response: Response): Promise<string | undefined> {
  try {
    const body: unknown = await response.json()
    const code = typeof body === 'object' && body !== null && 'code' in body ? body.code : undefined
    return typeof code === 'string' ? code : undefined
  } catch {
    return undefined
  }
}

export async function request<T>(
  auth: AuthContextValue,
  path: string,
  { method = 'GET', query, body, failureMessage }: RequestOptions
): Promise<T> {
  const headers = new Headers()
  await auth.authorizeRequest(headers)
  if (body !== undefined) headers.set('Content-Type', 'application/json')

  const response = await fetch(buildUrl(path, query), {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (!response.ok) {
    const code = await readErrorCode(response)
    throw new ApiError(domainErrorMessage(code) ?? failureMessage, response.status, code)
  }

  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}
