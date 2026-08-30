import { http } from './http'

export type InspectionStatus = '待执行' | '进行中' | '已完成'

export interface InspectionTask {
  taskId: number
  venueId: number
  venueName: string
  empId: number
  employeeName: string
  taskTime: string
  status: InspectionStatus
  remark?: string
}

export interface CreateInspectionTaskRequest {
  venueId: number
  empId: number
  taskTime: string
  remark?: string
}

export interface UpdateInspectionTaskStatusRequest {
  status: InspectionStatus
  remark?: string
}

export function getInspectionTasks(status?: InspectionStatus) {
  const params = new URLSearchParams({ pageNumber: '1', pageSize: '100' })
  if (status) {
    params.set('status', status)
  }

  return http.get<InspectionTask[]>(`/inspection-tasks?${params}`)
}

export function createInspectionTask(request: CreateInspectionTaskRequest) {
  return http.post<InspectionTask>('/inspection-tasks', request)
}

export function updateInspectionTaskStatus(
  taskId: number,
  request: UpdateInspectionTaskStatusRequest,
) {
  return http.patch<InspectionTask>(`/inspection-tasks/${taskId}/status`, request)
}
