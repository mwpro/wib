/**
 * Parses an API timestamp as UTC, also when the server omitted the trailing "Z".
 */
export function parseApiDate(dateInput: string): Date {
  const hasTimezone = /[Zz]$|[+-]\d{2}(:\d{2})?$/.test(dateInput)
  return new Date(hasTimezone ? dateInput : `${dateInput}Z`)
}
