import { http } from './http'

export interface AbsenceRecord {
  absenceId: number
  memberId: number
  bookingId: number
  courseName: string
  courseDate: string
  absenceTime: string | null
}

export function getMyAbsenceRecords(memberId: number) {
  return http.get<AbsenceRecord[]>(
    `/AbsenceRecords/member/${memberId}`
  )
}
