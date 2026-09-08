import { http } from './http'

export interface GroupCourseBooking {
  bookingId: number
  memberId: number
  packageId: number
  courseId: number
  courseName: string
  typeId: number
  courseTypeName: string
  bookingTime: string | null
  bookingStatus: string
  bookingStatusLabel: string
  courseStartTime?: string | null
  canCancel: boolean
  message: string
}

export interface GroupCourseBookingRequest {
  memberId: number
  courseId: number
  packageId: number
  courseDate?: string | null
}

export function bookGroupCourse(request: GroupCourseBookingRequest) {
  return http.post<GroupCourseBooking>('/GroupCourseBookings', request)
}

export function getMyGroupBookings(memberId: number) {
  return http.get<GroupCourseBooking[]>(`/GroupCourseBookings/member/${memberId}`)
}

export function cancelGroupCourse(memberId: number, courseId: number) {
  return http.delete(`/GroupCourseBookings?memberId=${memberId}&courseId=${courseId}`)
}

export function cancelGroupCourseById(bookingId: number, memberId: number) {
  return http.delete(`/GroupCourseBookings/${bookingId}?memberId=${memberId}`)
}
