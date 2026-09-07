import { http } from './http'

export interface VenueItem {
  venueId: number
  venueName: string
  maxCapacity: number
  currentCapacity?: number
  imageUrl?: string | null
  venueStatus?: string
}

export interface CreateVenueRequest {
  venueName: string
  maxCapacity: number
  imageUrl?: string | null
}

export interface UpdateVenueRequest {
  venueName: string
  maxCapacity: number
  venueStatus: '1' | '0'
  imageUrl?: string | null
}

export interface UploadVenueImageResult {
  imageUrl: string
}

export function getVenueManagementList(params: { keyword?: string; status?: 'all' | 'active' | 'inactive' } = {}) {
  const searchParams = new URLSearchParams()
  if (params.keyword) {
    searchParams.set('keyword', params.keyword)
  }
  if (params.status) {
    searchParams.set('status', params.status)
  }

  const query = searchParams.toString()
  return http.get<VenueItem[]>(`/venues${query ? `?${query}` : ''}`)
}

export function createVenue(payload: CreateVenueRequest) {
  return http.post<VenueItem>('/venues', payload)
}

export function updateVenue(venueId: number, payload: UpdateVenueRequest) {
  return http.put<VenueItem>(`/venues/${venueId}`, payload)
}

export async function uploadVenueImage(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  return http.post<UploadVenueImageResult>('/venues/upload-image', formData)
}

export function deleteVenue(venueId: number) {
  return http.delete(`/venues/${venueId}`)
}
