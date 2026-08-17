import { http } from './http'

export type LoginType = 'member' | 'coach'

export interface LoginRequest {
  loginType: LoginType
  identifier: string
  phoneNumber: string
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
