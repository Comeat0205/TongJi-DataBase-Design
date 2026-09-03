<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { registerMember } from '@/api/members'
import { login } from '@/api/auth'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const form = reactive({
  loginName: '',
  password: '',
  confirmPassword: '',
  name: '',
  phoneNumber: '',
  gender: '',
  birthday: '',
  idCard: '',
})

const isSubmitting = ref(false)
const errorMessage = ref('')

async function handleRegister() {
  errorMessage.value = ''

  if (!form.loginName.trim() || !form.password || !form.name.trim()) {
    errorMessage.value = '请填写登录名、密码和姓名。'
    return
  }

  if (form.password.length < 6) {
    errorMessage.value = '密码至少 6 位。'
    return
  }

  if (form.password !== form.confirmPassword) {
    errorMessage.value = '两次输入的密码不一致。'
    return
  }

  isSubmitting.value = true

  try {
    await registerMember({
      loginName: form.loginName.trim(),
      password: form.password,
      name: form.name.trim(),
      phoneNumber: form.phoneNumber.trim() || undefined,
      gender: form.gender || undefined,
      birthday: form.birthday || undefined,
      idCard: form.idCard.trim() || undefined,
    })

    const session = await login({
      loginType: 'member',
      loginName: form.loginName.trim(),
      password: form.password,
    })

    authStore.setSession(session)
    await router.push(session.targetPath)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '注册失败，请稍后重试。'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <main class="register-page">
    <section class="register-card">
      <p class="eyebrow">TJ-GYM · Member</p>
      <h1>会员注册</h1>
      <p class="intro">先创建登录账号（USERS），再写入会员档案（MEMBER）。注册成功后自动登录。</p>

      <form class="register-form" @submit.prevent="handleRegister">
        <label class="field">
          <span>登录名 *</span>
          <input v-model="form.loginName" type="text" autocomplete="username" placeholder="用于登录" />
        </label>
        <label class="field">
          <span>密码 *</span>
          <input v-model="form.password" type="password" autocomplete="new-password" placeholder="至少 6 位" />
        </label>
        <label class="field">
          <span>确认密码 *</span>
          <input v-model="form.confirmPassword" type="password" autocomplete="new-password" />
        </label>
        <label class="field">
          <span>姓名 *</span>
          <input v-model="form.name" type="text" placeholder="会员真实姓名" />
        </label>
        <label class="field">
          <span>手机号</span>
          <input v-model="form.phoneNumber" type="tel" />
        </label>
        <label class="field">
          <span>性别</span>
          <select v-model="form.gender">
            <option value="">请选择</option>
            <option value="M">男</option>
            <option value="F">女</option>
          </select>
        </label>
        <label class="field">
          <span>生日</span>
          <input v-model="form.birthday" type="date" />
        </label>
        <label class="field">
          <span>身份证号</span>
          <input v-model="form.idCard" type="text" />
        </label>

        <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>

        <button class="submit-btn" type="submit" :disabled="isSubmitting">
          {{ isSubmitting ? '提交中...' : '注册并登录' }}
        </button>
      </form>

      <p class="back-link">
        <RouterLink to="/login">已有账号？返回登录</RouterLink>
      </p>
    </section>
  </main>
</template>

<style scoped>
.register-page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 40px 20px;
  background: linear-gradient(135deg, #0b1220 0%, #121c31 45%, #192947 100%);
}

.register-card {
  width: min(520px, 100%);
  padding: 40px 32px;
  border-radius: 24px;
  background: rgba(255, 255, 255, 0.96);
  color: #182336;
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.28);
}

.eyebrow {
  margin: 0;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: #2c57d2;
  font-size: 0.8rem;
}

h1 {
  margin: 10px 0 0;
  font-size: 1.9rem;
}

.intro {
  margin: 12px 0 24px;
  color: #5b6b86;
  line-height: 1.6;
  font-size: 0.95rem;
}

.register-form {
  display: grid;
  gap: 14px;
}

.field {
  display: grid;
  gap: 6px;
}

.field span {
  font-size: 0.9rem;
  color: #4c5d78;
}

.field input,
.field select {
  width: 100%;
  border: 1px solid #d5deec;
  border-radius: 12px;
  padding: 12px 14px;
  font: inherit;
  background: #fff;
}

.error-message {
  margin: 0;
  color: #c0392b;
  font-size: 0.92rem;
}

.submit-btn {
  border: 0;
  border-radius: 12px;
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

.back-link {
  margin: 18px 0 0;
  text-align: center;
}

.back-link a {
  color: #2c57d2;
  text-decoration: none;
  font-weight: 600;
}
</style>
