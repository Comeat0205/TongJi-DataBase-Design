import { http } from './http'
import type { PaymentOrder } from './payment-orders'

export interface GroupPackage {
  packageId: number
  memberId: number
  courseId: number
  courseName: string
  typeId: number
  courseTypeName: string
  packageName: string
  coachName: string
  totalCount: number
  remainingCount: number
  packageStatus: string
  packageStatusLabel: string
  isUsable: boolean
}

export interface GroupPackageProduct {
  priceId: number
  productType: string
  typeId: number
  courseTypeName: string
  sessionCount: number
  price: number
  name: string
  isActive: boolean
}

export function getMyGroupPackages(memberId: number) {
  return http.get<GroupPackage[]>(`/group-packages/member/${memberId}`)
}

export function getGroupPackageProducts() {
  return http.get<GroupPackageProduct[]>('/group-packages/products')
}

export function getManageGroupPackageProducts() {
  return http.get<GroupPackageProduct[]>('/group-packages/products/manage')
}

export function createGroupPackageProduct(payload: {
  typeId: number
  sessionCount: number
  standardPrice: number
}) {
  return http.post<GroupPackageProduct>('/group-packages/products', payload)
}

export function patchGroupPackageProduct(
  priceId: number,
  payload: { sessionCount?: number; standardPrice?: number; isActive?: boolean },
) {
  return http.patch<GroupPackageProduct>(`/group-packages/products/${priceId}`, payload)
}

export function purchaseGroupPackage(payload: {
  memberId: number
  priceId: number
  courseId?: number | null
  voucherId?: number | null
}) {
  return http.post<PaymentOrder>('/group-packages/purchase', payload)
}
