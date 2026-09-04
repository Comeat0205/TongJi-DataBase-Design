import { http } from './http'

export interface MemberScheduleItem {
  scheduleId: number
  memberId: number
  scheduleStart: string
  scheduleDate: string
  scheduleEnd: string
  scheduleType: string
  sourceRecordId: number | null
  status: string | null
  isUpcoming: boolean
}

export function getMemberSchedules(memberId: number) {
  return http.get<MemberScheduleItem[]>(`/MemberSchedules?memberId=${memberId}`)
}
