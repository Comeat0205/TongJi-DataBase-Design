import axios from 'axios'

// 统一返回格式
export interface ApiResponse<T> {
  code: string
  message: string
  data: T
  traceId?: string
}

export class ApiError extends Error {
  code?: string
  traceId?: string

  constructor(message: string, code?: string, traceId?: string) {
    super(message)
    this.name = 'ApiError'
    this.code = code
    this.traceId = traceId
  }
}

const httpClient = axios.create({
  baseURL: '/api',
  timeout: 8000,
})

function getErrorMessage(error: unknown, fallbackMessage: string) {
  if (axios.isAxiosError<ApiResponse<unknown>>(error)) {
    return {
      message: error.response?.data?.message ?? fallbackMessage,
      code: error.response?.data?.code,
      traceId: error.response?.data?.traceId,
    }
  }

  return {
    message: fallbackMessage,
  }
}

async function request<T>(method: 'get' | 'post' | 'put' | 'patch' | 'delete', url: string, data?: unknown) {
  try {
    const response = await httpClient.request<ApiResponse<T>>({
      method,
      url,
      data,
    })

    return response.data.data
  } catch (error) {
    const apiError = getErrorMessage(error, '请求失败，请稍后重试。')
    throw new ApiError(apiError.message, apiError.code, apiError.traceId)
  }
}

export const http = {
  get<T>(url: string) {
    return request<T>('get', url)
  },
  post<T>(url: string, data?: unknown) {
    return request<T>('post', url, data)
  },
  put<T>(url: string, data?: unknown) {
    return request<T>('put', url, data)
  },
  patch<T>(url: string, data?: unknown) {
    return request<T>('patch', url, data)
  },
  delete<T>(url: string) {
    return request<T>('delete', url)
  },
}
