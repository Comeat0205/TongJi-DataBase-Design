import { http } from './http'

export interface PaymentOrder {
  orderId: number
  businessOrderId: number
  totalAmount: number
  discountValue: number
  payableAmount: number
  paymentStatus?: string
  createTime?: string
  paymentFinishTime?: string
  voucherId?: number
  voucherType?: string
  memberId?: number
  detailCount: number
  refundAmount?: number
  voucherRestored?: boolean
  actionMessage?: string
}

export function getPaymentOrders(params?: {
  memberId?: number
  businessOrderId?: number
  pageNumber?: number
  pageSize?: number
}) {
  const query = new URLSearchParams()
  if (params?.memberId != null) query.set('memberId', String(params.memberId))
  if (params?.businessOrderId != null) query.set('businessOrderId', String(params.businessOrderId))
  if (params?.pageNumber != null) query.set('pageNumber', String(params.pageNumber))
  if (params?.pageSize != null) query.set('pageSize', String(params.pageSize))
  const qs = query.toString()
  return http.get<PaymentOrder[]>(`/paymentorders${qs ? `?${qs}` : ''}`)
}

export function createPaymentOrder(payload: { memberId: number; totalAmount?: number; voucherId?: number | null }) {
  return http.post<PaymentOrder>('/paymentorders', payload)
}

export function updateOrderVoucher(
  orderId: number,
  voucherId: number | null,
  memberId?: number,
) {
  return http.put<PaymentOrder>(`/paymentorders/${orderId}/voucher`, {
    voucherId,
    memberId: memberId ?? undefined,
  })
}

export function payPaymentOrder(orderId: number) {
  return http.post<PaymentOrder>(`/paymentorders/${orderId}/pay`)
}

export function cancelPaymentOrder(orderId: number) {
  return http.post<PaymentOrder>(`/paymentorders/${orderId}/cancel`)
}
