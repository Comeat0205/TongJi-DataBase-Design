import { http } from './http'

export type RepairStatus = '待处理' | '维修中' | '已完成'

export interface RepairRecord {
  recordId: number
  equipId: number
  equipName: string
  empId?: number
  employeeName?: string
  reportTime?: string
  repairCost: number
  status: RepairStatus
  description?: string
}

export interface CreateRepairRecordRequest {
  equipId: number
  description: string
}

export interface UpdateRepairRecordStatusRequest {
  status: RepairStatus
  empId?: number
  repairCost?: number
}

export interface MaintenanceOption {
  id: number
  name: string
}

export interface RepairRecordOptions {
  equipment: MaintenanceOption[]
  employees: MaintenanceOption[]
}

export function getRepairRecordOptions() {
  return http.get<RepairRecordOptions>('/repair-records/options')
}

export function getRepairRecords(status?: RepairStatus) {
  const params = new URLSearchParams({ pageNumber: '1', pageSize: '100' })
  if (status) {
    params.set('status', status)
  }

  return http.get<RepairRecord[]>(`/repair-records?${params}`)
}

export function createRepairRecord(request: CreateRepairRecordRequest) {
  return http.post<RepairRecord>('/repair-records', request)
}

export function updateRepairRecordStatus(
  recordId: number,
  request: UpdateRepairRecordStatusRequest,
) {
  return http.patch<RepairRecord>(`/repair-records/${recordId}/status`, request)
}
