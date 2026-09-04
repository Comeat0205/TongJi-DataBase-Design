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

export interface MemberManagementListItem {
  userId: number
  memberId: number
  displayName: string
  realName: string
  phoneNumber?: string
  memberLevel?: string
  registerDate?: string
  status?: string
}

export interface UpdateMemberRequest {
  name: string
  phoneNumber?: string
  gender?: string
  birthday?: string
  idCard?: string
}

export interface RegisterMemberRequest {
  loginName: string
  password: string
  phoneNumber: string
  name: string
  idCard: string
}

export interface ValidateMemberRegistrationAccountRequest {
  loginName: string
  password: string
  phoneNumber: string
}

export interface GetMemberManagementListParams {
  keyword?: string
  sortBy?: 'userId' | 'memberId' | 'displayName' | 'registerDate'
  sortDirection?: 'asc' | 'desc'
}

export function getMemberProfile(memberId: number) {
  return http.get<MemberProfile>(`/members/${memberId}`)
}

export function getMemberManagementList(params: GetMemberManagementListParams = {}) {
  const searchParams = new URLSearchParams()

  if (params.keyword) {
    searchParams.set('keyword', params.keyword)
  }
  if (params.sortBy) {
    searchParams.set('sortBy', params.sortBy)
  }
  if (params.sortDirection) {
    searchParams.set('sortDirection', params.sortDirection)
  }

  const query = searchParams.toString()
  return http.get<MemberManagementListItem[]>(`/members${query ? `?${query}` : ''}`)
}

export function updateMember(memberId: number, payload: UpdateMemberRequest) {
  return http.put<MemberProfile>(`/members/${memberId}`, payload)
}

export function registerMember(payload: RegisterMemberRequest) {
  return http.post<MemberProfile>('/members', payload)
}

export function cancelMember(memberId: number) {
  return http.delete<MemberProfile>(`/members/${memberId}`)
}

export function validateMemberRegistrationAccount(payload: ValidateMemberRegistrationAccountRequest) {
  return http.post<null>('/members/registration/account-validation', payload)
}
