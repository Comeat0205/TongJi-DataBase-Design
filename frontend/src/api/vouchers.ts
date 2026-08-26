import { http } from './http'

export interface Voucher {
  voucherId: number
  memberId: number
  voucherType: string
  discountValue: number
  validUntil: string
  status?: string
  statusText: string
  isExpired: boolean
}

export function getVouchers(params?: { memberId?: number; pageNumber?: number; pageSize?: number }) {
  const query = new URLSearchParams()
  if (params?.memberId != null) query.set('memberId', String(params.memberId))
  if (params?.pageNumber != null) query.set('pageNumber', String(params.pageNumber))
  if (params?.pageSize != null) query.set('pageSize', String(params.pageSize))
  const qs = query.toString()
  return http.get<Voucher[]>(`/vouchers${qs ? `?${qs}` : ''}`)
}

export function getAvailableVouchers(memberId: number, forOrderId?: number) {
  const query = new URLSearchParams({ memberId: String(memberId) })
  if (forOrderId != null) query.set('forOrderId', String(forOrderId))
  return http.get<Voucher[]>(`/vouchers/available?${query.toString()}`)
}
