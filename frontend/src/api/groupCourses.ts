import { http } from './http'

export interface GroupCourse {
  courseId: number
  courseName: string
  maxCapacity: number
  currentCapacity: number
  courseSummary: string | null
  typeId: number
  coachId: number
  timeSlotId: string
}

export function getGroupCourses() {
  return http.get<GroupCourse[]>('/GroupCourses')
}