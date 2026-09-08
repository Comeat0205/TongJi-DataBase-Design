import { http } from './http'

export interface AtRiskMember {
  memberId: number
  name: string
  phoneNumber?: string
  memberLevel?: string
  lastCheckInTime?: string
  inactiveDays: number
  unusedVoucherCount: number
  riskReason: string
}

export function getAtRiskMembers(params?: { inactiveDays?: number; pageNumber?: number; pageSize?: number }) {
  const query = new URLSearchParams()
  if (params?.inactiveDays != null) query.set('inactiveDays', String(params.inactiveDays))
  if (params?.pageNumber != null) query.set('pageNumber', String(params.pageNumber))
  if (params?.pageSize != null) query.set('pageSize', String(params.pageSize))
  const qs = query.toString()
  return http.get<AtRiskMember[]>(`/at-risk-members${qs ? `?${qs}` : ''}`)
}
