import { http } from './http'

export type LoginType = 'member' | 'employee' | 'coach'

export interface LoginRequest {
  loginType: LoginType
  loginName: string
  password: string
}

export interface LoginResult {
  userType: LoginType
  userId: number
  displayName: string
  targetPath: string
}

export function login(request: LoginRequest) {
  return http.post<LoginResult>('/auth/login', request)
}
