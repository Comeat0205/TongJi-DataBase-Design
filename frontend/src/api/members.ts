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
