import { defineStore } from 'pinia'

export interface AuthSession {
  userType: 'member' | 'coach'
  userId: number
  displayName: string
  targetPath: string
}

const AUTH_STORAGE_KEY = 'tj-gym-auth-session'

function readSessionFromStorage(): AuthSession | null {
  const raw = localStorage.getItem(AUTH_STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as AuthSession
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
