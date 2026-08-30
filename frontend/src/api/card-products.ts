// 卡商品相关前端 API。

import { http } from './http'
import type { MembershipCard } from './membership-cards'

export interface CardProduct {
  priceId: number
  productType: string
  name: string
  cardType: string
  price: number
  description?: string
  isActive?: boolean
}

export interface PurchaseMembershipCardRequest {
  memberId: number
  priceId: number
}

export interface CreateCardProductRequest {
  productType: string
  standardPrice: number
}

export interface UpdateCardProductRequest {
  productType?: string
  standardPrice?: number
  isActive?: boolean
}

// 会员购卡页：仅在售商品
export function getCardProducts() {
  return http.get<CardProduct[]>('/card-products')
}

// 员工管理页：含下架商品
export function getManageCardProducts() {
  return http.get<CardProduct[]>('/card-products/manage')
}

// 新增商品
export function createCardProduct(payload: CreateCardProductRequest) {
  return http.post<CardProduct>('/card-products', payload)
}

// 全量更新商品
export function updateCardProduct(priceId: number, payload: UpdateCardProductRequest) {
  return http.put<CardProduct>(`/card-products/${priceId}`, payload)
}

// 部分更新（上架/下架、改价等）
export function patchCardProduct(priceId: number, payload: UpdateCardProductRequest) {
  return http.patch<CardProduct>(`/card-products/${priceId}`, payload)
}

// MVP 模拟支付成功购卡
export function purchaseMembershipCardMock(payload: PurchaseMembershipCardRequest) {
  return http.post<MembershipCard>('/membership-cards/purchase-mock', payload)
}
