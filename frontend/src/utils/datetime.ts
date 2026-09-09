/** 业务约定：全部按北京时间（Asia/Shanghai）展示与理解。 */

const BEIJING_TZ = 'Asia/Shanghai'

function hasExplicitTimeZone(value: string) {
  return /([zZ]|[+-]\d{2}:?\d{2})$/.test(value.trim())
}

/**
 * 解析接口时间：有 Z/偏移则按绝对时间；无时区则视为北京墙钟。
 */
export function parseBeijingDate(value: string | Date): Date {
  if (value instanceof Date) {
    return value
  }

  const raw = value.trim()
  if (!raw) {
    return new Date(Number.NaN)
  }

  if (hasExplicitTimeZone(raw)) {
    return new Date(raw)
  }

  // 日期或日期时间无时区：按东八区墙钟解释，避免被当成浏览器本地/UTC
  if (/^\d{4}-\d{2}-\d{2}$/.test(raw)) {
    return new Date(`${raw}T00:00:00+08:00`)
  }

  const normalized = raw.includes('T') ? raw : raw.replace(' ', 'T')
  return new Date(`${normalized}+08:00`)
}

export function formatBeijingDateTime(
  value?: string | Date | null,
  options?: Intl.DateTimeFormatOptions,
): string {
  if (value == null || value === '') {
    return '—'
  }

  const date = parseBeijingDate(value)
  if (Number.isNaN(date.getTime())) {
    return String(value)
  }

  return date.toLocaleString('zh-CN', {
    timeZone: BEIJING_TZ,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
    ...options,
  })
}

export function formatBeijingDate(value?: string | Date | null): string {
  if (value == null || value === '') {
    return '—'
  }

  const date = parseBeijingDate(value)
  if (Number.isNaN(date.getTime())) {
    return String(value)
  }

  return date.toLocaleDateString('zh-CN', {
    timeZone: BEIJING_TZ,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  })
}
