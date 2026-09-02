import { http } from './http'

export interface CoachManagementListItem {
  coachId: number
  userId: number
  displayName: string
  coachName: string
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
