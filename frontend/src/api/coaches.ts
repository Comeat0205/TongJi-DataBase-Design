import { http } from './http'

export interface CoachManagementListItem {
  coachId: number
  userId: number
  displayName: string
  coachName: string
  loginName?: string
  phoneNumber?: string
  sex?: string
  specialty?: string
  hireDate?: string
  coachSummary?: string
  status?: string
}

export interface GetCoachManagementListParams {
  keyword?: string
  sortBy?: 'coachId' | 'userId' | 'displayName' | 'coachName' | 'hireDate'
  sortDirection?: 'asc' | 'desc'
  status?: 'all' | 'active' | 'inactive'
}

export interface CreateCoachRequest {
  loginName: string
  password: string
  displayName: string
  coachName: string
  phoneNumber?: string
  sex?: string
  specialty?: string
  coachSummary?: string
}

export interface UpdateCoachRequest {
  displayName: string
  coachName: string
  phoneNumber?: string
  sex?: string
  specialty?: string
  coachSummary?: string
}

export function getCoachManagementList(params: GetCoachManagementListParams = {}) {
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
  return http.get<CoachManagementListItem[]>(`/coaches${query ? `?${query}` : ''}`)
}

export function getCoachDetail(coachId: number) {
  return http.get<CoachManagementListItem>(`/coaches/${coachId}`)
}

export function createCoach(payload: CreateCoachRequest) {
  return http.post<CoachManagementListItem>('/coaches', payload)
}

export function updateCoach(coachId: number, payload: UpdateCoachRequest) {
  return http.put<CoachManagementListItem>(`/coaches/${coachId}`, payload)
}

export function deactivateCoach(coachId: number) {
  return http.delete<CoachManagementListItem>(`/coaches/${coachId}`)
}
