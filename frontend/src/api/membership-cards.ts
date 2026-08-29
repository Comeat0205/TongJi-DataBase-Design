// 会员卡相关前端 API，对应后端 MembershipCardsController。

import { http } from './http'

export interface MembershipCard {
  cardId: number
  memberId: number
  cardType: string
  cardTypeLabel: string
  cardStatus?: string
  issueDate?: string
  totalCounts?: number
  remainingCount?: number
  expireDate?: string
  isValid: boolean
}

// 查某个会员名下的所有卡
export function getMyCards(memberId: number) {
  return http.get<MembershipCard[]>(`/membership-cards?memberId=${memberId}`)
}

// 查单张卡详情
export function getCardById(cardId: number) {
  return http.get<MembershipCard>(`/membership-cards/${cardId}`)
}
