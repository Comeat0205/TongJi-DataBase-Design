import { defineStore } from 'pinia'

export interface AuthSession {
  userType: 'member' | 'coach' | 'employee'
  userId: number
  displayName: string
  targetPath: string
}

const AUTH_STORAGE_KEY = 'tj-gym-auth-session'

const DEFAULT_TARGET_BY_TYPE: Record<AuthSession['userType'], string> = {
  member: '/member/home',
  employee: '/admin/home',
  coach: '/coach/home',
}

function normalizeSession(session: AuthSession): AuthSession {
  const defaultTarget = DEFAULT_TARGET_BY_TYPE[session.userType]

  if (session.userType === 'member' && session.targetPath.includes('/member/profile/')) {
    return { ...session, targetPath: defaultTarget }
  }

  if (!session.targetPath.startsWith(`/${session.userType === 'employee' ? 'admin' : session.userType}/`)) {
    return { ...session, targetPath: defaultTarget }
  }

  return session
}

function readSessionFromStorage(): AuthSession | null {
  const raw = localStorage.getItem(AUTH_STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    const session = normalizeSession(JSON.parse(raw) as AuthSession)
    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session))
    return session
  } catch {
    localStorage.removeItem(AUTH_STORAGE_KEY)
    return null
  }
}

export function hasStoredAuthSession(): AuthSession | null {
  return readSessionFromStorage()
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    session: readSessionFromStorage() as AuthSession | null,
  }),
  getters: {
    isAuthenticated: (state) => !!state.session,
  },
  actions: {
    setSession(session: AuthSession) {
      this.session = session
      localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(session))
    },
    clearSession() {
      this.session = null
      localStorage.removeItem(AUTH_STORAGE_KEY)
    },
  },
})
