import { http } from './http'

export interface PersonalPackage {
  packageId: number
  memberId: number
  coachId: number
  coachName: string
  personalCourseId: number
  courseName: string
  courseDescription?: string
  totalSessions: number
  remainingSessions: number
  expireDate: string
  packageStatus: string
  isUsable: boolean
}

export function getMemberPersonalPackages(memberId: number) {
  return http.get<PersonalPackage[]>(`/members/${memberId}/personal-packages`)
}
