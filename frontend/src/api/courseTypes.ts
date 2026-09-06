import { http } from './http'

export interface CourseType {
  typeId: number
  typeName: string
}

export interface CourseTypeRequest {
  typeId: number
  typeName: string
}

export function getCourseTypes() {
  return http.get<CourseType[]>('/course-types')
}

export function createCourseType(data: CourseTypeRequest) {
  return http.post<CourseType>('/course-types', data)
}

export function updateCourseType(typeId: number, data: CourseTypeRequest) {
  return http.put<CourseType>(`/course-types/${typeId}`, data)
}

export function deleteCourseType(typeId: number) {
  return http.delete<{ message: string }>(`/course-types/${typeId}`)
}
