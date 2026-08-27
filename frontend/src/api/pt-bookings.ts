import { http } from './http'

export type PtBookingStatus = 'PENDING' | 'CONFIRMED' | 'REJECTED' | 'CANCELLED'
export type PtConsumeStatus = '0' | '1'

export interface PtBooking {
  ptBookingId: number
  packageId: number
  memberId: number
  coachId: number
  coachName: string
  courseName: string
  bookingTime: string
  sessionTime: string
  coachConfirmed: string
  memberConfirmed: string
  consumeStatus: PtConsumeStatus
  consumedTime?: string | null
  status: PtBookingStatus
  isConsumed: boolean
  canConsume: boolean
  canUndoConsumption: boolean
}

export interface CreatePtBookingRequest {
  memberId: number
  packageId: number
  sessionTime: string
}

export function getMemberPtBookings(memberId: number) {
  return http.get<PtBooking[]>(`/members/${memberId}/pt-bookings`)
}

export function getPendingCoachPtBookings(coachId: number) {
  return http.get<PtBooking[]>(`/coaches/${coachId}/pt-bookings/pending`)
}

export function getCoachPtBookings(coachId: number) {
  return http.get<PtBooking[]>(`/coaches/${coachId}/pt-bookings`)
}

export function createPtBooking(request: CreatePtBookingRequest) {
  return http.post<PtBooking>('/pt-bookings', request)
}

export function cancelPtBooking(bookingId: number, memberId: number) {
  return http.delete<void>(`/pt-bookings/${bookingId}?memberId=${memberId}`)
}

export function confirmPtBooking(bookingId: number, coachId: number, accept: boolean) {
  return http.post<void>(`/pt-bookings/${bookingId}/confirm`, { coachId, accept })
}

export function consumePtBooking(bookingId: number, coachId: number) {
  return http.post<void>(`/pt-bookings/${bookingId}/consume`, { coachId })
}

export function undoPtBookingConsumption(bookingId: number, coachId: number) {
  return http.post<void>(`/pt-bookings/${bookingId}/undo-consume`, { coachId })
}
