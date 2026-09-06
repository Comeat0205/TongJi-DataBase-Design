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
  timeSlotId: string
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
  coachId: number
  courseDate: string
  startTime: string
  endTime: string
}

export interface GroupCourseScheduleConflictResponse {
  code: string
  message: string
  data: unknown
  traceId: string | null
}

export function checkGroupCourseScheduleConflict(
  courseId: number,
  request: GroupCourseScheduleConflictRequest,
) {
  return http.post<GroupCourseScheduleConflictResponse>(
    `/GroupCourses/${courseId}/schedule/check`,
    request,
  )
}