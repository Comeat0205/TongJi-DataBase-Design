import type { RouteRecordRaw } from 'vue-router'

const Placeholder = () => import('@/views/shared/ModulePlaceholderView.vue')
const MemberHome = () => import('@/views/member/MemberHomeView.vue')
const MemberProfile = () => import('@/views/member/MemberProfileView.vue')
const GroupCourseList = () => import('@/views/member/GroupCourseListView.vue')
const AdminHome = () => import('@/views/admin/AdminHomeView.vue')
const CoachHome = () => import('@/views/coach/CoachHomeView.vue')

type PortalPrefix = 'member' | 'admin' | 'coach'
type RouteMode = 'auth' | 'preview'

interface PlaceholderRoute {
  path: string
  name: string
  pageTitle: string
  pageSubtitle?: string
  owner?: string
  features?: string
  eyebrow?: string
}

function placeholderChildRoutes(prefix: PortalPrefix, mode: RouteMode, items: PlaceholderRoute[]): RouteRecordRaw[] {
  return items.map((item) => ({
    path: item.path,
    name: item.name,
    component: Placeholder,
    meta: {
      pageTitle: item.pageTitle,
      pageSubtitle: item.pageSubtitle,
      owner: item.owner,
      features: item.features,
      eyebrow: item.eyebrow,
      preview: mode === 'preview',
      userType: prefix === 'member' ? 'member' : prefix === 'admin' ? 'employee' : 'coach',
    },
  }))
}

function memberChildren(mode: RouteMode): RouteRecordRaw[] {
  const p = mode === 'preview' ? 'preview-member' : 'member'

  return [
    {
      path: 'home',
      name: `${p}-home`,
      component: MemberHome,
      meta: { userType: 'member', preview: mode === 'preview' },
    },
    {
      path: 'profile/:id/edit',
      name: `${p}-profile-edit`,
      component: Placeholder,
      meta: {
        pageTitle: '编辑会员资料',
        pageSubtitle: '会员自助维护姓名、联系方式等信息。',
        owner: 'B',
        features: '#1',
        preview: mode === 'preview',
        userType: 'member',
      },
    },
    {
      path: 'profile/:id',
      name: `${p}-profile`,
      component: MemberProfile,
      meta: { requiresAuth: mode === 'auth', userType: 'member', preview: mode === 'preview' },
    },
    ...placeholderChildRoutes('member', mode, [
      {
        path: 'cards',
        name: `${p}-cards`,
        pageTitle: '我的会员卡',
        owner: 'D',
        features: '#1 #5 #6 #20',
      },
      {
        path: 'card-products',
        name: `${p}-card-products`,
        pageTitle: '购买会员卡',
        owner: 'D',
        features: '#20',
      },
      {
        path: 'check-in',
        name: `${p}-check-in`,
        pageTitle: '入场签到',
        owner: 'E',
        features: '#5 #6 #7 #18',
      },
      {
        path: 'pt-packages',
        name: `${p}-pt-packages`,
        pageTitle: '私教课包',
        owner: 'G',
        features: '#12',
      },
      {
        path: 'pt-bookings',
        name: `${p}-pt-bookings`,
        pageTitle: '私教预约',
        owner: 'G',
        features: '#12 #13 #14',
      },
      {
        path: 'schedule',
        name: `${p}-schedule`,
        pageTitle: '我的日程',
        owner: 'J',
        features: '#11 #13',
      },
      {
        path: 'orders',
        name: `${p}-orders`,
        pageTitle: '我的订单',
        owner: 'H',
        features: '#20',
      },
      {
        path: 'vouchers',
        name: `${p}-vouchers`,
        pageTitle: '我的优惠券',
        owner: 'H',
        features: '#18',
      },
    ]),
    {
      path: 'group-courses',
      name: `${p}-group-courses`,
      component: GroupCourseList,
      meta: {
        pageTitle: '团课预约',
        owner: 'F',
        features: '#4 #8 #9 #19',
        preview: mode === 'preview',
        userType: 'member',
      },
    },
    {
      path: 'my-group-bookings',
      name: `${p}-my-group-bookings`,
      component: Placeholder,
      meta: {
        pageTitle: '我的团课预约',
        owner: 'F',
        features: '#8 #9 #10 #11',
        preview: mode === 'preview',
        userType: 'member',
      },
    },
  ]
}

function adminChildren(mode: RouteMode): RouteRecordRaw[] {
  const p = mode === 'preview' ? 'preview-admin' : 'admin'

  return [
    {
      path: 'home',
      name: `${p}-home`,
      component: AdminHome,
      meta: { userType: 'employee', preview: mode === 'preview' },
    },
    ...placeholderChildRoutes('admin', mode, [
    {
      path: 'check-in-desk',
      name: `${p}-check-in-desk`,
      pageTitle: '前台入场',
      owner: 'E',
      features: '#5 #6 #7',
    },
    {
      path: 'capacity-logs',
      name: `${p}-capacity-logs`,
      pageTitle: '容量日志',
      owner: 'E',
      features: '#7 #21',
    },
    {
      path: 'members',
      name: `${p}-members`,
      pageTitle: '会员管理',
      owner: 'C',
      features: '#1 #2 #17',
    },
    {
      path: 'coaches',
      name: `${p}-coaches`,
      pageTitle: '教练管理',
      owner: 'C',
      features: '#3 #4',
    },
    {
      path: 'course-types',
      name: `${p}-course-types`,
      pageTitle: '课程类型维护',
      owner: 'C / F',
      features: '#3 #4',
    },
    {
      path: 'venues',
      name: `${p}-venues`,
      pageTitle: '场馆管理',
      owner: 'C',
    },
    {
      path: 'equipment',
      name: `${p}-equipment`,
      pageTitle: '器材管理',
      owner: 'C',
      features: '#15',
    },
    {
      path: 'card-products',
      name: `${p}-card-products`,
      pageTitle: '卡商品管理',
      owner: 'D',
    },
    {
      path: 'group-courses',
      name: `${p}-group-courses`,
      pageTitle: '团课排期管理',
      owner: 'F',
      features: '#3 #4',
    },
    {
      path: 'orders',
      name: `${p}-orders`,
      pageTitle: '订单管理',
      owner: 'H',
    },
    {
      path: 'vouchers',
      name: `${p}-vouchers`,
      pageTitle: '优惠券管理',
      owner: 'H',
      features: '#18 #20',
    },
    {
      path: 'repairs',
      name: `${p}-repairs`,
      pageTitle: '器材报修',
      owner: 'I',
      features: '#15',
    },
    {
      path: 'inspections',
      name: `${p}-inspections`,
      pageTitle: '巡检任务',
      owner: 'I',
      features: '#16',
    },
    {
      path: 'at-risk-members',
      name: `${p}-at-risk-members`,
      pageTitle: '流失预警会员',
      owner: 'H',
      features: '#17',
    },
    ]),
  ]
}

function coachChildren(mode: RouteMode): RouteRecordRaw[] {
  const p = mode === 'preview' ? 'preview-coach' : 'coach'

  return [
    {
      path: 'home',
      name: `${p}-home`,
      component: CoachHome,
      meta: { userType: 'coach', preview: mode === 'preview' },
    },
    ...placeholderChildRoutes('coach', mode, [
      {
        path: 'schedule',
        name: `${p}-schedule`,
        pageTitle: '教练日程',
        owner: 'J',
        features: '#4 #11 #13',
      },
      {
        path: 'pt-confirm',
        name: `${p}-pt-confirm`,
        pageTitle: '私教确认与消课',
        owner: 'G',
        features: '#13 #14',
      },
    ]),
  ]
}

function portalRoute(
  basePath: string,
  layout: RouteRecordRaw['component'],
  layoutProps: Record<string, unknown>,
  meta: Record<string, unknown>,
  children: RouteRecordRaw[],
): RouteRecordRaw {
  return {
    path: basePath,
    component: layout,
    props: layoutProps,
    meta,
    children: [{ path: '', redirect: `${basePath}/home` }, ...children],
  }
}

export function buildPortalRoutes(options: {
  memberLayout: RouteRecordRaw['component']
  adminLayout: RouteRecordRaw['component']
  coachLayout: RouteRecordRaw['component']
}): RouteRecordRaw[] {
  return [
    portalRoute('/member', options.memberLayout, {}, { requiresAuth: true, userType: 'member' }, memberChildren('auth')),
    portalRoute('/admin', options.adminLayout, {}, { requiresAuth: true, userType: 'employee' }, adminChildren('auth')),
    portalRoute('/coach', options.coachLayout, {}, { requiresAuth: true, userType: 'coach' }, coachChildren('auth')),
    portalRoute(
      '/preview/member',
      options.memberLayout,
      { preview: true },
      { preview: true },
      memberChildren('preview'),
    ),
    portalRoute(
      '/preview/admin',
      options.adminLayout,
      { preview: true },
      { preview: true },
      adminChildren('preview'),
    ),
    portalRoute(
      '/preview/coach',
      options.coachLayout,
      { preview: true },
      { preview: true },
      coachChildren('preview'),
    ),
  ]
}
