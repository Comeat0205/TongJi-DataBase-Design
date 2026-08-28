import { http } from './http'

export interface CheckInRequest {
  cardId: number
  venueId: number
}

export interface CheckInResult {
  checkInOutId: number
  memberName: string
  venueName: string
  checkInTime: string
  cardType: string
  cardStatus: string
  remainingCount?: number
  expireDate?: string
}

export interface CheckInOutRecord {
  checkInOutId: number
  venueId: number
  venueName: string
  cardId?: number
  memberId?: number
  memberName?: string
  checkInTime: string
  checkOutTime?: string
  checkOutMode?: string
}

export interface VenueStatus {
  venueId: number
  venueName: string
  maxCapacity: number
  currentCapacity: number
  occupancyRate: number
  venueStatus: string
  capacityWarningLevel: string
}

// 入场
export function checkIn(req: CheckInRequest) {
  return http.post<CheckInResult>('/checkinout/check-in', req)
}

// 退场
export function checkOut(id: number) {
  return http.post<CheckInOutRecord>(`/checkinout/${id}/check-out`)
}

// 场馆容量
export function getVenueStatus() {
  return http.get<VenueStatus[]>('/checkinout/venues')
}

// 在场人员
export function getActiveCheckIns(venueId: number) {
  return http.get<CheckInOutRecord[]>(`/checkinout/active/${venueId}`)
}

// 入场记录分页
export function getCheckInRecords(venueId = 0, page = 1, size = 20) {
  const p = new URLSearchParams({ venueId: String(venueId), pageNumber: String(page), pageSize: String(size) })
  return http.get<CheckInOutRecord[]>(`/checkinout/records?${p}`)
}

// ---- 员工首页统计 ----

export interface DashboardStats {
  todayCheckIns: number
  activeMembers: number
  venues: VenueStatus[]
}

export function getDashboardStats() {
  return http.get<DashboardStats>('/checkinout/dashboard-stats')
}

// 手动触发自动签退（演示/测试用）
export function triggerAutoCheckout() {
  return http.post<{ message: string }>('/checkinout/auto-checkout')
}

// 会员查询自己的在场记录
export function getMyCheckIn(cardId: number) {
  return http.get<CheckInOutRecord | null>(`/checkinout/my-checkin/${cardId}`)
}
