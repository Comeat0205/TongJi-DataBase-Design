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

export interface GroupCourseRequest {
  courseId: number
  courseName: string
  maxCapacity: number
  courseSummary: string | null
  typeId: number
  coachId: number
  timeSlotId?: string | null
  /** 1=周一 … 7=周日 */
  weekday?: number | null
  /** HH:mm */
  startTime?: string | null
  /** HH:mm */
  endTime?: string | null
  scheduleFrom?: string | null
  scheduleTo?: string | null
}

export function getGroupCourses() {
  return http.get<GroupCourse[]>('/GroupCourses')
}

export function createGroupCourse(request: GroupCourseRequest) {
  return http.post<GroupCourse>('/GroupCourses', request)
}

export function updateGroupCourse(
  courseId: number,
  request: GroupCourseRequest,
) {
  return http.put<GroupCourse>(`/GroupCourses/${courseId}`, request)
}

export function deleteGroupCourse(courseId: number) {
  return http.delete<object>(`/GroupCourses/${courseId}`)
}

export interface GroupCourseScheduleConflictRequest {
  courseId?: number
  coachId: number
  rangeStart: string
  rangeEnd: string
  /** 1=周一 … 7=周日 */
  weekday?: number
  /** HH:mm */
  startTime?: string
  /** HH:mm */
  endTime?: string
}

/** 创建/修改前冲突检测（可带 weekday/时段做草稿检测） */
export function checkGroupCourseScheduleConflict(
  request: GroupCourseScheduleConflictRequest,
) {
  return http.post<string>(`/GroupCourses/schedule/check`, request)
}
