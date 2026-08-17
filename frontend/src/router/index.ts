import { createRouter, createWebHistory } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import { hasStoredAuthSession } from '../stores/auth'

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
    {
      path: '/member/profile/:id',
      name: 'member-profile',
      component: () => import('../views/MemberProfileView.vue'),
      meta: { requiresAuth: true, userType: 'member' },
    },
  ],
})

router.beforeEach((to) => {
  const session = hasStoredAuthSession()

  if (to.meta.requiresAuth && !session) {
    return { name: 'login' }
  }

  if (to.meta.guestOnly && session) {
    return session.targetPath
  }

  if (to.meta.userType && session?.userType !== to.meta.userType) {
    return session?.targetPath ?? { name: 'login' }
  }

  return true
})

export default router
