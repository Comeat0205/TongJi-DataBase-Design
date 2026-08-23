<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError } from '../api/http'
import { login, type LoginType } from '../api/auth'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const selectedLoginType = ref<LoginType>('member')
const form = reactive({
  identifier: '',
  phoneNumber: '',
})
const isSubmitting = ref(false)
const errorMessage = ref('')

const submitLabel = computed(() => {
  switch (selectedLoginType.value) {
    case 'member':
      return '会员登录'
    case 'employee':
      return '员工登录'
    case 'coach':
      return '教练登录'
  }
})

const identifierLabel = computed(() => {
  switch (selectedLoginType.value) {
    case 'member':
      return '用户名 / 会员ID'
    case 'employee':
      return '姓名 / 员工ID'
    case 'coach':
      return '姓名 / 教练ID'
  }
})

async function handleLogin() {
  errorMessage.value = ''

  if (!form.identifier.trim() || !form.phoneNumber.trim()) {
    errorMessage.value = '请输入账号标识和手机号。'
    return
  }

  isSubmitting.value = true

  try {
    const result = await login({
      loginType: selectedLoginType.value,
      identifier: form.identifier,
      phoneNumber: form.phoneNumber,
    })

    authStore.setSession(result)
    await router.push(result.targetPath)
  } catch (error) {
    errorMessage.value =
      error instanceof ApiError ? error.message : '登录失败，请检查账号和手机号是否正确。'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <main class="login-page">
    <section class="login-shell">
      <div class="brand-panel">
        <p class="eyebrow">TJ-GYM</p>
        <h1>欢迎登录</h1>
        <p class="intro">请输入账号信息以进入健身房管理系统。</p>
      </div>

      <section class="login-card">
        <div class="login-switch">
          <button
            type="button"
            class="switch-btn"
            :class="{ active: selectedLoginType === 'member' }"
            @click="selectedLoginType = 'member'"
          >
            会员
          </button>
          <button
            type="button"
            class="switch-btn"
            :class="{ active: selectedLoginType === 'employee' }"
            @click="selectedLoginType = 'employee'"
          >
            员工
          </button>
          <button
            type="button"
            class="switch-btn"
            :class="{ active: selectedLoginType === 'coach' }"
            @click="selectedLoginType = 'coach'"
          >
            教练
          </button>
        </div>

        <form class="login-form" @submit.prevent="handleLogin">
          <label class="field">
            <span>{{ identifierLabel }}</span>
            <input
              v-model="form.identifier"
              type="text"
              placeholder="请输入用户名或用户ID"
              autocomplete="username"
            />
          </label>

          <label class="field">
            <span>手机号</span>
            <input
              v-model="form.phoneNumber"
              type="text"
              placeholder="请输入登记手机号"
              autocomplete="tel"
            />
          </label>

          <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>

          <button class="submit-btn" type="submit" :disabled="isSubmitting">
            {{ isSubmitting ? '登录中...' : submitLabel }}
          </button>
        </form>

        <div class="preview-links">
          <p>也可先预览三端页面骨架（无需登录，仅看 Layout 与占位页）：</p>
          <div class="preview-actions">
            <RouterLink to="/preview/member/home">会员端预览</RouterLink>
            <RouterLink to="/preview/admin/home">员工端预览</RouterLink>
            <RouterLink to="/preview/coach/home">教练端预览</RouterLink>
          </div>
        </div>
      </section>
    </section>
  </main>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 40px;
  background:
    radial-gradient(circle at top left, rgba(76, 122, 255, 0.18), transparent 32%),
    radial-gradient(circle at bottom right, rgba(90, 206, 255, 0.16), transparent 28%),
    linear-gradient(135deg, #0b1220 0%, #121c31 45%, #192947 100%);
}

.login-shell {
  width: min(1240px, 100%);
  display: grid;
  grid-template-columns: minmax(520px, 1.2fr) minmax(420px, 0.9fr);
  overflow: hidden;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 28px;
  background: rgba(7, 13, 24, 0.76);
  backdrop-filter: blur(18px);
  box-shadow: 0 30px 80px rgba(0, 0, 0, 0.35);
}

.brand-panel,
.login-card {
  padding: 56px;
}

.brand-panel {
  display: flex;
  flex-direction: column;
  justify-content: center;
  color: #f8fbff;
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.02), rgba(255, 255, 255, 0.06));
}

.eyebrow {
  margin: 0 0 16px;
  font-size: 13px;
  letter-spacing: 0.28em;
  text-transform: uppercase;
  color: #89b3ff;
}

.brand-panel h1 {
  margin: 0;
  font-size: clamp(34px, 4vw, 48px);
  line-height: 1.1;
}

.intro {
  margin: 18px 0 0;
  max-width: 420px;
  font-size: 17px;
  line-height: 1.75;
  color: rgba(232, 239, 255, 0.78);
}

.login-card {
  display: flex;
  flex-direction: column;
  justify-content: center;
  background: rgba(250, 252, 255, 0.96);
}

.login-card {
  min-width: 0;
}

.login-switch {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
  padding: 6px;
  border-radius: 16px;
  background: #eef3fb;
}

.switch-btn {
  border: none;
  border-radius: 12px;
  background: transparent;
  color: #4f5f7a;
  font-size: 15px;
  font-weight: 600;
  padding: 14px 16px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.switch-btn.active {
  background: #16233b;
  color: #fff;
  box-shadow: 0 10px 24px rgba(22, 35, 59, 0.18);
}

.switch-btn.ghost:not(.active) {
  color: #7a889f;
}

.login-form {
  display: grid;
  gap: 18px;
  margin-top: 28px;
}

.field {
  display: grid;
  gap: 10px;
}

.field span {
  color: #27344b;
  font-size: 14px;
  font-weight: 600;
}

.field input {
  width: 100%;
  border: 1px solid #d9e2f0;
  border-radius: 16px;
  padding: 15px 16px;
  font-size: 15px;
  background: #fff;
  color: #182336;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.field input:focus {
  outline: none;
  border-color: #5f8bff;
  box-shadow: 0 0 0 4px rgba(95, 139, 255, 0.14);
}

.error-message {
  margin: 0;
  color: #d73c4f;
  font-size: 14px;
}

.submit-btn {
  margin-top: 4px;
  border: none;
  border-radius: 16px;
  padding: 16px 18px;
  background: linear-gradient(135deg, #285cff 0%, #5d8dff 100%);
  color: #fff;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 16px 30px rgba(40, 92, 255, 0.24);
}

.submit-btn:disabled {
  cursor: not-allowed;
  opacity: 0.75;
}

.preview-links {
  margin-top: 28px;
  padding-top: 22px;
  border-top: 1px solid #e6edf8;
}

.preview-links p {
  margin: 0 0 12px;
  color: #5d6d88;
  font-size: 14px;
  line-height: 1.6;
}

.preview-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.preview-actions a {
  padding: 10px 14px;
  border-radius: 12px;
  background: #eef3fb;
  color: #285cff;
  font-size: 14px;
  font-weight: 600;
}

@media (min-width: 1280px) {
  .login-shell {
    grid-template-columns: minmax(600px, 1.25fr) minmax(460px, 0.85fr);
  }
}

@media (max-width: 900px) {
  .login-shell {
    grid-template-columns: 1fr;
  }

  .brand-panel,
  .login-card {
    padding: 28px;
  }
}
</style>
