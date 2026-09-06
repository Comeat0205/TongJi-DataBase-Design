import { http } from './http'

export interface Coach {
  coachId: number
  coachName: string
  specialty: string | null
  status: string | null
}

export function getCoaches() {
  return http.get<Coach[]>('/coaches')
}
