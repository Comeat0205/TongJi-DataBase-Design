import { http } from './http'

export interface GroupCourseBooking {
  bookingId: number
  memberId: number
  courseId: number
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