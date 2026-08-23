export interface NavItem {
  path: string
  label: string
  /** 是否允许匹配子路径（如档案编辑页） */
  matchChildren?: boolean
}

/** 预览模式下档案菜单使用的演示会员 ID（与 docs 中 /api/members/1 示例一致） */
export const PREVIEW_MEMBER_ID = 1

export function getMemberNav(memberId?: number): NavItem[] {
  const items: NavItem[] = [
    { path: '/member/home', label: '首页' },
    { path: '/member/cards', label: '我的会员卡' },
    { path: '/member/card-products', label: '购买会员卡' },
    { path: '/member/check-in', label: '入场签到' },
    { path: '/member/group-courses', label: '团课预约' },
    { path: '/member/my-group-bookings', label: '我的团课' },
    { path: '/member/pt-packages', label: '私教课包' },
    { path: '/member/pt-bookings', label: '私教预约' },
    { path: '/member/schedule', label: '我的日程' },
    { path: '/member/orders', label: '我的订单' },
    { path: '/member/vouchers', label: '优惠券' },
  ]

  if (memberId) {
    items.splice(1, 0, {
      path: `/member/profile/${memberId}`,
      label: '我的档案',
      matchChildren: true,
    })
  }

  return items
}

export const adminNav: NavItem[] = [
  { path: '/admin/home', label: '工作台' },
  { path: '/admin/check-in-desk', label: '前台入场' },
  { path: '/admin/capacity-logs', label: '容量日志' },
  { path: '/admin/members', label: '会员管理' },
  { path: '/admin/coaches', label: '教练管理' },
  { path: '/admin/course-types', label: '课程类型' },
  { path: '/admin/group-courses', label: '团课排期' },
  { path: '/admin/venues', label: '场馆管理' },
  { path: '/admin/equipment', label: '器材管理' },
  { path: '/admin/repairs', label: '器材报修' },
  { path: '/admin/inspections', label: '巡检任务' },
  { path: '/admin/card-products', label: '卡商品' },
  { path: '/admin/orders', label: '订单管理' },
  { path: '/admin/vouchers', label: '优惠券' },
  { path: '/admin/at-risk-members', label: '流失预警' },
]

export const coachNav: NavItem[] = [
  { path: '/coach/home', label: '工作台' },
  { path: '/coach/schedule', label: '我的日程', matchChildren: true },
  { path: '/coach/pt-confirm', label: '私教确认', matchChildren: true },
]

export function getPreviewMemberNav(memberId?: number): NavItem[] {
  return getMemberNav(memberId).map((item) => ({
    ...item,
    path: item.path.replace('/member/', '/preview/member/'),
  }))
}

export function getPreviewAdminNav(): NavItem[] {
  return adminNav.map((item) => ({
    ...item,
    path: item.path.replace('/admin/', '/preview/admin/'),
  }))
}

export function getPreviewCoachNav(): NavItem[] {
  return coachNav.map((item) => ({
    ...item,
    path: item.path.replace('/coach/', '/preview/coach/'),
  }))
}

export function isNavItemActive(item: NavItem, currentPath: string) {
  if (currentPath === item.path) {
    return true
  }

  if (item.matchChildren) {
    return currentPath.startsWith(`${item.path}/`)
  }

  return false
}
