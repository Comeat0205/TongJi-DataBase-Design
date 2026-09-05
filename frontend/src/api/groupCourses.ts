import { http } from './http'

export interface GroupCourseTimeSlot {
  courseDate: string
  startTime: string
  endTime: string
}

export interface GroupCourse {
  courseId: number
  courseName: string
  maxCapacity: number
  currentCapacity: number
  courseSummary: string | null

  typeId: number
  courseTypeName: string

  coachId: number
  coachName: string

  timeSlotId: string
  timeSlots: GroupCourseTimeSlot[]
}

export function getGroupCourses() {
  return http.get<GroupCourse[]>('/GroupCourses')
}