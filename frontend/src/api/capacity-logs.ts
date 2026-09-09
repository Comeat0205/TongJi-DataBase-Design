import { http } from './http'

export interface CapacityLog {
  capacityLogId: number
  venueId: number
  venueName: string
  logTimestamp?: string
  recordedCapacity?: number
  recordedCount: number
  occupancyRate?: number
}

export interface CapacityLogPoint {
  timestamp: string
  timeLabel: string
  recordedCount: number
  occupancyRate: number
  recordedCapacity?: number
}

export interface CapacityDailySeries {
  venueId: number
  venueName: string
  date: string
  maxCapacity: number
  points: CapacityLogPoint[]
}

export interface CapacityMovement {
  movementId: number
  venueId: number
  venueName: string
  memberId: number
  eventTime: string
  eventTypeLabel: string
  eventType: string
  recordedCount: number
  occupancyRate: number
}

/** 主训练馆按日容量波形数据（每 10 分钟采样） */
export function getDailyCapacitySeries(date: string) {
  const params = new URLSearchParams({ date })
  return http.get<CapacityDailySeries>(`/checkinout/capacity-logs/daily?${params}`)
}

/** 主训练馆按日签到/签退流水 */
export function getDailyCapacityMovements(date: string) {
  const params = new URLSearchParams({ date })
  return http.get<CapacityMovement[]>(`/checkinout/capacity-logs/movements?${params}`)
}
