import { http } from './http'

export interface GroupCourseBooking {
  bookingId: number
  memberId: number
  courseId: number
  courseName: string
  bookingTime: string | null
  bookingStatus: string
  message: string
}

export interface GroupCourseBookingRequest {
  memberId: number
  courseId: number
}

export function bookGroupCourse(request: GroupCourseBookingRequest) {
  return http.post<GroupCourseBooking>(
    '/GroupCourseBookings',
    request,
  )
}

export function getMyGroupBookings(memberId: number) {
  return http.get<GroupCourseBooking[]>(
    `/GroupCourseBookings/member/${memberId}`,
  )
}

export function cancelGroupCourse(
  memberId: number,
  courseId: number,
) {
  return http.delete(
    `/GroupCourseBookings?memberId=${memberId}&courseId=${courseId}`,
  )
}