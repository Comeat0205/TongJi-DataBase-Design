import { http } from './http'

export interface WaitingQueueRequest {
  memberId: number
  courseId: number
}

export interface WaitingQueue {
  queueId: number
  memberId: number
  courseId: number
  courseName: string
  enqueueTime: string | null
  queueStatus: string
  message: string
}

export function joinWaitingQueue(request: WaitingQueueRequest) {
  return http.post<WaitingQueue>(
    '/WaitingQueues',
    request,
  )
}

export function getMyWaitingQueues(memberId: number) {
  return http.get<WaitingQueue[]>(
    `/WaitingQueues/member/${memberId}`,
  )
}
