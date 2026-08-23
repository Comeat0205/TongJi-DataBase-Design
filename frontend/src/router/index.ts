import { createRouter, createWebHistory } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import MemberLayout from '@/layouts/MemberLayout.vue'
import AdminLayout from '@/layouts/AdminLayout.vue'
import CoachLayout from '@/layouts/CoachLayout.vue'
import { hasStoredAuthSession } from '../stores/auth'
import { buildPortalRoutes } from './routes'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      redirect: '/login',
    },
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { guestOnly: true },
    },
    ...buildPortalRoutes({
      memberLayout: MemberLayout,
      adminLayout: AdminLayout,
      coachLayout: CoachLayout,
    }),
  ],
})

router.beforeEach((to) => {
  const session = hasStoredAuthSession()
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)
  const isPreview = to.matched.some((record) => record.meta.preview)
  const userType = [...to.matched].reverse().find((record) => record.meta.userType)?.meta.userType as
    | 'member'
    | 'coach'
    | 'employee'
    | undefined

  if (isPreview) {
    return true
  }

  if (requiresAuth && !session) {
    return { name: 'login' }
  }

  if (to.meta.guestOnly && session) {
    return session.targetPath
  }

  if (userType && session?.userType !== userType) {
    return session?.targetPath ?? { name: 'login' }
  }

  return true
})

export default router
