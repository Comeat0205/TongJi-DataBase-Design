import { http } from './http'
import type { PaymentOrder } from './payment-orders'

export interface PersonalPackage {
  packageId: number
  memberId: number
  coachId: number
  coachName: string
  personalCourseId: number
  courseName: string
  courseDescription?: string
  totalSessions: number
  remainingSessions: number
  expireDate: string
  packageStatus: string
  isUsable: boolean
}

export interface PersonalPackageProduct {
  priceId: number
  productType: string
  personalCourseId: number
  courseName: string
  courseDescription?: string
  coachId: number
  coachName: string
  totalSessions: number
  validDays: number
  price: number
  isActive: boolean
}

export interface PurchasePersonalPackageRequest {
  memberId: number
  priceId: number
  voucherId?: number | null
}

export function getMemberPersonalPackages(memberId: number) {
  return http.get<PersonalPackage[]>(`/members/${memberId}/personal-packages`)
}

export function getPersonalPackageProducts() {
  return http.get<PersonalPackageProduct[]>('/personal-package-products')
}

export function purchasePersonalPackage(payload: PurchasePersonalPackageRequest) {
  return http.post<PaymentOrder>('/personal-packages/purchase', payload)
}
