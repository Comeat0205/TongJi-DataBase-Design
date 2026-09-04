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

export const VOUCHER_TYPE_BIRTHDAY = '生日福利券'
export const VOUCHER_TYPE_WELCOME = '新客体验券'
export const VOUCHER_TYPE_DISCOUNT = '折扣券'

export function getVouchers(params?: {
  memberId?: number
  voucherType?: string
  pageNumber?: number
  pageSize?: number
}) {
  const query = new URLSearchParams()
  if (params?.memberId != null) query.set('memberId', String(params.memberId))
  if (params?.voucherType) query.set('voucherType', params.voucherType)
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

export function issueDiscountVoucher(memberId: number) {
  return http.post<Voucher>('/vouchers/issue-discount', { memberId })
}

export function issueDiscountVoucherToAll() {
  return http.post<number>('/vouchers/issue-discount-all')
}

export function issueWelcomeVoucher(memberId: number) {
  return http.post<Voucher>(`/vouchers/issue-welcome/${memberId}`)
}

export function issueBirthdayVouchersToday() {
  return http.post<number>('/vouchers/issue-birthday-today')
}
