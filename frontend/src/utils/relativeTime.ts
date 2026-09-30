/**
 * Formats a timestamp into a Polish relative time string (e.g. "przed chwilą", "15 min temu", "wczoraj").
 */
export function formatRelativeTimePl(dateInput: string | Date): string {
  let date: Date
  if (typeof dateInput === 'string') {
    const hasTimezone = /[Zz]$|[+-]\d{2}(:\d{2})?$/.test(dateInput)
    date = new Date(hasTimezone ? dateInput : `${dateInput}Z`)
  } else {
    date = dateInput
  }
  const now = new Date()
  const diffInSeconds = Math.max(0, Math.floor((now.getTime() - date.getTime()) / 1000))

  if (diffInSeconds < 60) {
    return 'przed chwilą'
  }

  const diffInMinutes = Math.floor(diffInSeconds / 60)
  if (diffInMinutes < 60) {
    return `${diffInMinutes} min temu`
  }

  const diffInHours = Math.floor(diffInMinutes / 60)
  if (diffInHours < 24) {
    if (diffInHours === 1) return '1 godz. temu'
    return `${diffInHours} godz. temu`
  }

  const diffInDays = Math.floor(diffInHours / 24)
  if (diffInDays === 1) {
    return 'wczoraj'
  }
  if (diffInDays < 30) {
    return `${diffInDays} dni temu`
  }

  return date.toLocaleDateString('pl-PL', {
    day: 'numeric',
    month: 'short',
  })
}
