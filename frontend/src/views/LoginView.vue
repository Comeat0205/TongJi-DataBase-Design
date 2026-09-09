<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError } from '../api/http'
import { login, type LoginType } from '../api/auth'
import { useAuthStore } from '../stores/auth'
import loginBg from '../assets/login-bg.png'

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

  return '登录'
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
    <div class="bg-layer" aria-hidden="true">
      <div class="bg-image" :style="{ backgroundImage: `url(${loginBg})` }" />
      <div class="bg-blur" />
      <div class="bg-veil" />
    </div>

    <section class="login-shell">
      <div class="brand-panel">
        <div class="brand-flow" aria-hidden="true">
          <span class="orb orb-a" />
          <span class="orb orb-b" />
          <span class="orb orb-c" />
          <span class="orb orb-d" />
        </div>
        <div class="brand-content">
          <p class="eyebrow">TJ-GYM</p>
          <h1>欢迎来到TJ-GYM</h1>
          <p class="slogan">汗水铸就力量，坚持定义改变。</p>
          <p class="intro">
            请选择身份后，使用登录名与密码进入对应工作台。会员可先注册账号；员工与教练账号由管理员开通。
          </p>
        </div>
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
      </section>
    </section>
  </main>
</template>

<style scoped>
.login-page {
  --navy: #081e3a;
  --sky: #6ba6db;
  --mist: #f4f8fd;
  position: relative;
  isolation: isolate;
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 40px;
  overflow: hidden;
}

.bg-layer {
  position: absolute;
  inset: 0;
  z-index: -1;
}

.bg-image {
  position: absolute;
  inset: -12px;
  background-position: center;
  background-size: cover;
  background-repeat: no-repeat;
  transform: scale(1.03);
  filter: blur(2.5px) saturate(1.08) brightness(0.92);
}

.bg-blur {
  position: absolute;
  inset: 0;
  backdrop-filter: blur(0.5px);
  background: rgba(8, 30, 58, 0.12);
}

.bg-veil {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at 18% 20%, rgba(107, 166, 219, 0.16), transparent 42%),
    radial-gradient(circle at 82% 78%, rgba(8, 30, 58, 0.28), transparent 46%),
    linear-gradient(135deg, rgba(8, 30, 58, 0.28), rgba(8, 30, 58, 0.1) 48%, rgba(8, 30, 58, 0.22));
}

.login-shell {
  width: min(1080px, 100%);
  min-height: min(680px, calc(100vh - 80px));
  display: grid;
  grid-template-columns: minmax(0, 0.95fr) minmax(360px, 0.85fr);
  overflow: hidden;
  border: 1px solid rgba(255, 255, 255, 0.28);
  border-radius: 28px;
  background: rgba(255, 255, 255, 0.12);
  backdrop-filter: blur(10px);
  box-shadow: 0 30px 80px rgba(8, 30, 58, 0.35);
}

.brand-panel,
.login-card {
  padding: 64px 52px;
}

.brand-panel {
  position: relative;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  justify-content: center;
  min-height: 100%;
  color: #f8fbff;
  background: #081e3a;
}

.brand-flow {
  position: absolute;
  inset: -20%;
  pointer-events: none;
}

.orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(36px);
  opacity: 0.55;
  mix-blend-mode: screen;
  animation: drift 14s ease-in-out infinite;
}

.orb-a {
  width: 300px;
  height: 300px;
  left: -10%;
  top: 6%;
  background: radial-gradient(circle, rgba(107, 166, 219, 0.85) 0%, rgba(107, 166, 219, 0) 70%);
  animation-duration: 16s;
}

.orb-b {
  width: 360px;
  height: 360px;
  right: -14%;
  bottom: -12%;
  background: radial-gradient(circle, rgba(107, 166, 219, 0.55) 0%, rgba(107, 166, 219, 0) 72%);
  animation-duration: 18s;
  animation-direction: reverse;
}

.orb-c {
  width: 200px;
  height: 200px;
  left: 46%;
  top: 16%;
  background: radial-gradient(circle, rgba(107, 166, 219, 0.7) 0%, rgba(107, 166, 219, 0) 70%);
  animation-duration: 12s;
  animation-delay: -3s;
}

.orb-d {
  width: 240px;
  height: 240px;
  left: 16%;
  bottom: 10%;
  background: radial-gradient(circle, rgba(107, 166, 219, 0.48) 0%, rgba(107, 166, 219, 0) 70%);
  animation-duration: 15s;
  animation-delay: -6s;
  animation-direction: reverse;
}

@keyframes drift {
  0%,
  100% {
    transform: translate3d(0, 0, 0) scale(1);
  }
  33% {
    transform: translate3d(28px, -36px, 0) scale(1.08);
  }
  66% {
    transform: translate3d(-34px, 22px, 0) scale(0.94);
  }
}

.brand-content {
  position: relative;
  z-index: 1;
}

.eyebrow {
  margin: 0 0 12px;
  letter-spacing: 0.22em;
  text-transform: uppercase;
  color: rgba(244, 248, 253, 0.82);
  font-size: 0.85rem;
}

.brand-panel h1 {
  margin: 0;
  font-size: clamp(2.2rem, 3.8vw, 3.3rem);
  line-height: 1.15;
  letter-spacing: 0.02em;
}

.slogan {
  margin: 18px 0 0;
  font-size: 1.15rem;
  font-weight: 600;
  color: #e8f2fb;
  letter-spacing: 0.04em;
}

.intro {
  margin: 14px 0 0;
  max-width: 30rem;
  color: rgba(244, 248, 253, 0.78);
  line-height: 1.75;
}

.login-card {
  display: flex;
  flex-direction: column;
  justify-content: center;
  min-height: 100%;
  background: rgba(255, 255, 255, 0.94);
  color: #182336;
}

.login-switch {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 10px;
  width: 100%;
  margin-bottom: 36px;
  padding: 8px;
  border-radius: 16px;
  background: #e8eef6;
}

.switch-btn {
  border: 0;
  border-radius: 12px;
  padding: 14px 8px;
  background: var(--mist);
  color: #5b6b86;
  cursor: pointer;
  font-weight: 600;
  transition: background 0.2s ease, color 0.2s ease, box-shadow 0.2s ease;
}

.switch-btn.active {
  background: var(--navy);
  color: #fff;
  box-shadow: 0 8px 18px rgba(8, 30, 58, 0.22);
}

.login-form {
  display: grid;
  width: 100%;
  gap: 24px;
}

.field {
  display: grid;
  gap: 10px;
}

.field span {
  font-size: 0.95rem;
  color: #4c5d78;
}

.field input {
  width: 100%;
  box-sizing: border-box;
  border: 1px solid #d7e3f0;
  border-radius: 14px;
  padding: 16px 18px;
  font: inherit;
  background: var(--mist);
  color: #182336;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.field input:focus {
  outline: none;
  border-color: var(--sky);
  box-shadow: 0 0 0 3px rgba(107, 166, 219, 0.28);
}

.error-message {
  min-height: 1.4em;
  margin: 0;
  color: #c0392b;
  font-size: 0.92rem;
  line-height: 1.4;
}

.submit-btn {
  width: 100%;
  border: 0;
  border-radius: 14px;
  padding: 16px 18px;
  background: var(--navy);
  color: #fff;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.2s ease, opacity 0.2s ease;
  box-shadow: 0 10px 22px rgba(8, 30, 58, 0.22);
}

.submit-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 14px 26px rgba(8, 30, 58, 0.28);
}

.submit-btn:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.register-link {
  min-height: 1.4em;
  margin-top: 28px;
  text-align: center;
  line-height: 1.4;
}

.register-link a {
  color: var(--navy);
  text-decoration: none;
  font-weight: 600;
}

@media (max-width: 960px) {
  .login-shell {
    grid-template-columns: 1fr;
    min-height: auto;
    width: min(520px, 100%);
  }

  .brand-panel {
    min-height: 280px;
    padding: 40px 28px;
  }

  .login-card {
    padding: 36px 28px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .orb {
    animation: none;
  }
}
</style>
