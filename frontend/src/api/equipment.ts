import { http } from './http'

export interface EquipmentItem {
  equipId: number
  equipName: string
  venueId?: string | null
  imageUrl?: string | null
  status?: string
  purchaseDate?: string
}

export interface CreateEquipmentRequest {
  equipName: string
  venueId?: string | null
  imageUrl?: string | null
}

export interface UpdateEquipmentRequest {
  equipName: string
  venueId?: string | null
  imageUrl?: string | null
  status: '1' | '0'
}

export interface UploadEquipmentImageResult {
  imageUrl: string
}

export function getEquipmentManagementList(params: { keyword?: string; status?: 'all' | 'active' | 'inactive'; venueId?: number } = {}) {
  const searchParams = new URLSearchParams()
  if (params.keyword) {
    searchParams.set('keyword', params.keyword)
  }
  if (params.status) {
    searchParams.set('status', params.status)
  }
  if (params.venueId !== undefined) {
    searchParams.set('venueId', String(params.venueId))
  }

  const query = searchParams.toString()
  return http.get<EquipmentItem[]>(`/equipment${query ? `?${query}` : ''}`)
}

export function createEquipment(payload: CreateEquipmentRequest) {
  return http.post<EquipmentItem>('/equipment', payload)
}

export function updateEquipment(equipId: number, payload: UpdateEquipmentRequest) {
  return http.put<EquipmentItem>(`/equipment/${equipId}`, payload)
}

export async function uploadEquipmentImage(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  return http.post<UploadEquipmentImageResult>('/equipment/upload-image', formData)
}

export function deleteEquipment(equipId: number) {
  return http.delete(`/equipment/${equipId}`)
}
