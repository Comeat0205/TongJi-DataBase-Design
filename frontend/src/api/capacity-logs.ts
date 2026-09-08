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

/** 分页查询容量日志 */
export function getCapacityLogs(venueId = 0, pageNumber = 1, pageSize = 20) {
  const params = new URLSearchParams({
    venueId: String(venueId),
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  })
  return http.get<CapacityLog[]>(`/checkinout/capacity-logs?${params}`)
}
