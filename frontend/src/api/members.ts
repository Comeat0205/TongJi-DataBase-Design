import { http } from './http'

export interface MemberProfile {
  memberId: number
  name: string
  phoneNumber?: string
  idCard?: string
  memberLevel?: string
  gender?: string
  birthday?: string
  registerDate?: string
  status?: string
}

export function getMemberProfile(memberId: number) {
  return http.get<MemberProfile>(`/members/${memberId}`)
}

export function getMembers(params?: { pageNumber?: number; pageSize?: number }) {
  const query = new URLSearchParams()
  if (params?.pageNumber != null) query.set('pageNumber', String(params.pageNumber))
  if (params?.pageSize != null) query.set('pageSize', String(params.pageSize))
  const qs = query.toString()
  return http.get<MemberProfile[]>(`/members${qs ? `?${qs}` : ''}`)
}
