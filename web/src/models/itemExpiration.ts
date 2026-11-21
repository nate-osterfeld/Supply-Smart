type ItemExpiry = { expirationDate: string; expirationTimestamp?: string | null }

export function expirationInputValue(item: ItemExpiry): string {
  const value = item.expirationTimestamp || item.expirationDate
  if (!value || value === 'N/A') return ''
  if (/^\d{4}-\d{2}-\d{2}(?:T|$)/.test(value)) return value.slice(0, 10)

  // Older API responses contain only a display date, interpreted as a local calendar day.
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const year = String(date.getFullYear()).padStart(4, '0')
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function expirationForSave(day: string, original?: string | null): string | null {
  if (!day) return null
  if (original && original.slice(0, 10) === day) return original
  return `${day}T00:00:00.000Z`
}
