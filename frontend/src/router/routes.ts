import type { RouteRecordRaw } from 'vue-router'

const Placeholder = () => import('@/views/shared/ModulePlaceholderView.vue')
const MemberHome = () => import('@/views/member/MemberHomeView.vue')
const MemberProfile = () => import('@/views/member/MemberProfileView.vue')
const MembershipCardList = () => import('@/views/member/MembershipCardListView.vue')
const CardProductList = () => import('@/views/member/CardProductListView.vue')
const CardProductManage = () => import('@/views/admin/CardProductManageView.vue')
const MemberIdentityEdit = () => import('@/views/member/MemberIdentityEditView.vue')
const PersonalPackageList = () => import('@/views/member/PersonalPackageListView.vue')
const PtBooking = () => import('@/views/member/PtBookingView.vue')
const AdminHome = () => import('@/views/admin/AdminHomeView.vue')
const AdminMembers = () => import('@/views/admin/MembersView.vue')
const AdminCoachList = () => import('@/views/admin/CoachListView.vue')
const AdminMemberDetail = () => import('@/views/admin/MemberDetailView.vue')
const AdminVenues = () => import('@/views/admin/VenuesView.vue')
const AdminEquipment = () => import('@/views/admin/EquipmentView.vue')
const CoachHome = () => import('@/views/coach/CoachHomeView.vue')
const CheckIn = () => import('@/views/member/CheckInView.vue')
const CheckInDesk = () => import('@/views/admin/CheckInDeskView.vue')
const CapacityLogs = () => import('@/views/admin/CapacityLogsView.vue')
const PtConfirm = () => import('@/views/coach/PtConfirmView.vue')

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
      component: MemberIdentityEdit,
      meta: {
        requiresAuth: mode === 'auth',
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
    


    // feature/venue-checkin 入场与容量模块
    {
      path: 'check-in',
      name: `${p}-check-in`,
      component: CheckIn,
      meta: { pageTitle: '签到签退', owner: 'E', features: '#5 #6 #7 #18', preview: mode === 'preview', userType: 'member' },
    },



    // feature/membership-card 会员卡与会籍模块
   {
      path: 'cards',
      name: `${p}-cards`,
      component: MembershipCardList,
      meta: {
        requiresAuth: mode === 'auth',
        userType: 'member',
        preview: mode === 'preview',
        pageTitle: '我的会员卡',
        owner: 'D',
        features: '#1 #5 #6 #20',
      },
    },
    {
      path: 'card-products',
      name: `${p}-card-products`,
      component: CardProductList,
      meta: {
        requiresAuth: mode === 'auth',
        userType: 'member',
        preview: mode === 'preview',
        pageTitle: '购买会员卡',
        owner: 'D',
        features: '#20',
      },
    },



    // feature/personal-training  私教课包与预约模块
    {
      path: 'pt-packages',
      name: `${p}-pt-packages`,
      component: PersonalPackageList,
      meta: { requiresAuth: mode === 'auth', userType: 'member', preview: mode === 'preview' },
    },
    {
      path: 'pt-bookings',
      name: `${p}-pt-bookings`,
      component: PtBooking,
      meta: { requiresAuth: mode === 'auth', userType: 'member', preview: mode === 'preview' },
    },


    
    ...placeholderChildRoutes('member', mode, [
      {
        path: 'group-courses',
        name: `${p}-group-courses`,
        pageTitle: '团课预约',
        owner: 'F',
        features: '#4 #8 #9 #19',
      },
      {
        path: 'my-group-bookings',
        name: `${p}-my-group-bookings`,
        pageTitle: '我的团课预约',
        owner: 'F',
        features: '#8 #9 #10 #11',
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



    // feature/venue-checkin 入场与容量模块
    {
      path: 'check-in-desk',
      name: `${p}-check-in-desk`,
      component: CheckInDesk,
      meta: { pageTitle: '前台入场', owner: 'E', features: '#5 #6 #7', preview: mode === 'preview', userType: 'employee' },
    },
    {
      path: 'capacity-logs',
      name: `${p}-capacity-logs`,
      component: CapacityLogs,
      meta: { pageTitle: '容量日志', owner: 'E', features: '#7 #21', preview: mode === 'preview', userType: 'employee' },
    },



    // feature/basic-info  基本信息模块
     {
      path: 'members',
      name: `${p}-members`,
      component: AdminMembers,
      meta: { requiresAuth: mode === 'auth', userType: 'employee', preview: mode === 'preview' },
    },
    {
      path: 'members/:id',
      name: `${p}-member-detail`,
      component: AdminMemberDetail,
      meta: { requiresAuth: mode === 'auth', userType: 'employee', preview: mode === 'preview' },
    },
    {
      path: 'coaches',
      name: `${p}-coaches`,
      component: AdminCoachList,
      meta: { requiresAuth: mode === 'auth', userType: 'employee', preview: mode === 'preview' },
    },
    {
      path: 'venues',
      name: `${p}-venues`,
      component: AdminVenues,
      meta: { requiresAuth: mode === 'auth', userType: 'employee', preview: mode === 'preview' },
    },
    {
      path: 'equipment',
      name: `${p}-equipment`,
      component: AdminEquipment,
      meta: { requiresAuth: mode === 'auth', userType: 'employee', preview: mode === 'preview' },
    },



     // feature/membership-card 会员卡与会籍模块
    {
      path: 'card-products',
      name: `${p}-card-products`,
      component: CardProductManage,
      meta: {
        userType: 'employee',
        preview: mode === 'preview',
        pageTitle: '卡商品管理',
        owner: 'D',
      },
    },
   


    ...placeholderChildRoutes('admin', mode, [
      {
        path: 'course-types',
        name: `${p}-course-types`,
        pageTitle: '课程类型维护',
        owner: 'C / F',
        features: '#3 #4',
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
    
    

    // feature/personal-training  私教课包与预约模块
    {
      path: 'pt-confirm',
      name: `${p}-pt-confirm`,
      component: PtConfirm,
      meta: { requiresAuth: mode === 'auth', userType: 'coach', preview: mode === 'preview' },
    },



    ...placeholderChildRoutes('coach', mode, [
      {
        path: 'schedule',
        name: `${p}-schedule`,
        pageTitle: '教练日程',
        owner: 'J',
        features: '#4 #11 #13',
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
