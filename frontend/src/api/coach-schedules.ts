import { http } from './http'

export interface CoachScheduleItem {
  scheduleId: number
  coachId: number
  scheduleStart: string
  scheduleEnd: string
  scheduleDate: string
  scheduleType: string | null
  sourceRecordId: number | null
  status: string | null
  isConflict: boolean
  memberId?: number | null
  memberName?: string | null
  courseName?: string | null
}

export function getCoachSchedules(coachId: number) {
  return http.get<CoachScheduleItem[]>(`/CoachSchedules?coachId=${coachId}`)
}
