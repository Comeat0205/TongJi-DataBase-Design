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
  loginName: '',
  password: '',
})
const isSubmitting = ref(false)
const errorMessage = ref('')
const canSelfRegister = computed(() => selectedLoginType.value === 'member')

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

async function handleLogin() {
  errorMessage.value = ''

  if (!form.loginName.trim() || !form.password) {
    errorMessage.value = '请输入登录名和密码。'
    return
  }

  isSubmitting.value = true

  try {
    const result = await login({
      loginType: selectedLoginType.value,
      loginName: form.loginName,
      password: form.password,
    })

    authStore.setSession(result)
    await router.push(result.targetPath)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '账号或密码错误'
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
        <p class="intro">使用登录名与密码进入对应工作台。会员可先注册账号。</p>
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
            <span>登录名</span>
            <input
              v-model="form.loginName"
              type="text"
              placeholder="请输入登录名"
              autocomplete="username"
            />
          </label>

          <label class="field">
            <span>密码</span>
            <input
              v-model="form.password"
              type="password"
              placeholder="请输入密码"
              autocomplete="current-password"
            />
          </label>

          <p class="error-message" aria-live="polite">{{ errorMessage }}</p>

          <button class="submit-btn" type="submit" :disabled="isSubmitting">
            {{ isSubmitting ? '登录中...' : submitLabel }}
          </button>
        </form>

        <div class="register-link">
          <RouterLink v-if="canSelfRegister" to="/register">没有账号？会员注册</RouterLink>
        </div>

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
}

.eyebrow {
  margin: 0 0 12px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: #8fb0ff;
  font-size: 0.85rem;
}

.brand-panel h1 {
  margin: 0;
  font-size: clamp(2.4rem, 4vw, 3.6rem);
  line-height: 1.1;
}

.intro {
  margin: 18px 0 0;
  max-width: 28rem;
  color: rgba(248, 251, 255, 0.72);
  line-height: 1.7;
}

.login-card {
  background: rgba(255, 255, 255, 0.96);
  color: #182336;
}

.login-switch {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
  margin-bottom: 28px;
  padding: 6px;
  border-radius: 16px;
  background: #eef3fb;
}

.switch-btn {
  border: 0;
  border-radius: 12px;
  padding: 12px 8px;
  background: transparent;
  color: #5b6b86;
  cursor: pointer;
  font-weight: 600;
}

.switch-btn.active {
  background: #16233b;
  color: #fff;
}

.login-form {
  display: grid;
  gap: 18px;
}

.field {
  display: grid;
  gap: 8px;
}

.field span {
  font-size: 0.92rem;
  color: #4c5d78;
}

.field input {
  width: 100%;
  border: 1px solid #d5deec;
  border-radius: 14px;
  padding: 14px 16px;
  font: inherit;
  background: #fff;
}

.error-message {
  min-height: 1.4em;
  margin: 0;
  color: #c0392b;
  font-size: 0.92rem;
  line-height: 1.4;
}

.submit-btn {
  border: 0;
  border-radius: 14px;
  padding: 14px 18px;
  background: #16233b;
  color: #fff;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.submit-btn:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.register-link {
  min-height: 1.4em;
  margin-top: 18px;
  text-align: center;
  line-height: 1.4;
}

.register-link a {
  color: #2c57d2;
  text-decoration: none;
  font-weight: 600;
}

.preview-links {
  margin-top: 28px;
  padding-top: 22px;
  border-top: 1px solid #e4ebf5;
}

.preview-links p {
  margin: 0 0 12px;
  color: #667892;
  font-size: 0.9rem;
}

.preview-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.preview-actions a {
  color: #16233b;
  text-decoration: none;
  font-weight: 600;
}

@media (max-width: 960px) {
  .login-shell {
    grid-template-columns: 1fr;
  }

  .brand-panel,
  .login-card {
    padding: 36px 28px;
  }
}
</style>
