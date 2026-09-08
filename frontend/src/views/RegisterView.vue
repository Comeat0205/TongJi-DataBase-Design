<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { registerMember, validateMemberRegistrationAccount } from '@/api/members'

type RegistrationStep = 'account' | 'identity'

const phoneRegex = /^1[3-9]\d{9}$/
const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/
const idCardRegex = /^\d{17}[\dXx]$/
const idCardWeights = [7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2]
const idCardCheckCodes = ['1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2']

const router = useRouter()

const currentStep = ref<RegistrationStep>('account')
const isSubmitting = ref(false)
const errorMessage = ref('')

const form = reactive({
  loginName: '',
  password: '',
  confirmPassword: '',
  phoneNumber: '',
  name: '',
  idCard: '',
})

const identityPreview = computed(() => {
  const idCard = form.idCard.trim().toUpperCase()
  if (!idCard) {
    return null
  }

  try {
    return parseIdentityCard(idCard)
  } catch {
    return null
  }
})

const derivedBirthday = computed(() => identityPreview.value?.birthday ?? '')
const derivedGender = computed(() => identityPreview.value?.genderLabel ?? '')

function parseIdentityCard(idCard: string) {
  if (!idCardRegex.test(idCard)) {
    throw new Error('请输入合法的 18 位身份证号。')
  }

  const birthdayValue = idCard.slice(6, 14)
  const year = Number(birthdayValue.slice(0, 4))
  const month = Number(birthdayValue.slice(4, 6))
  const day = Number(birthdayValue.slice(6, 8))
  const birthday = new Date(Date.UTC(year, month - 1, day))

  if (
    Number.isNaN(birthday.getTime()) ||
    birthday.getUTCFullYear() !== year ||
    birthday.getUTCMonth() !== month - 1 ||
    birthday.getUTCDate() !== day
  ) {
    throw new Error('身份证号中的出生日期无效。')
  }

  const sum = idCardWeights.reduce((total, weight, index) => total + Number(idCard[index]) * weight, 0)
  const expectedCheckCode = idCardCheckCodes[sum % 11]
  if (idCard[17] !== expectedCheckCode) {
    throw new Error('身份证号校验失败，请检查后重试。')
  }

  const gender = Number(idCard[16]) % 2 === 1 ? 'M' : 'F'
  return {
    birthday: `${year.toString().padStart(4, '0')}-${month.toString().padStart(2, '0')}-${day.toString().padStart(2, '0')}`,
    gender,
    genderLabel: gender === 'M' ? '男' : '女',
  }
}

function validateAccountStep() {
  if (!form.loginName.trim()) {
    throw new Error('请输入登录名。')
  }

  if (form.loginName.trim().length > 50) {
    throw new Error('登录名长度不能超过 50 个字符。')
  }

  if (!passwordRegex.test(form.password)) {
    throw new Error('密码至少 8 位，且必须同时包含大写字母、小写字母和数字。')
  }

  if (form.password !== form.confirmPassword) {
    throw new Error('两次输入的密码不一致。')
  }

  if (!phoneRegex.test(form.phoneNumber.trim())) {
    throw new Error('请输入合法的 11 位手机号。')
  }
}

function validateIdentityStep() {
  if (!form.name.trim()) {
    throw new Error('请输入真实姓名。')
  }

  const idCard = form.idCard.trim().toUpperCase()
  if (!idCard) {
    throw new Error('请输入身份证号。')
  }

  parseIdentityCard(idCard)
}

async function goToIdentityStep() {
  errorMessage.value = ''

  try {
    validateAccountStep()
    isSubmitting.value = true

    await validateMemberRegistrationAccount({
      loginName: form.loginName.trim(),
      password: form.password,
      phoneNumber: form.phoneNumber.trim(),
    })

    currentStep.value = 'identity'
  } catch (error) {
    errorMessage.value = error instanceof ApiError || error instanceof Error ? error.message : '校验失败，请稍后重试。'
  } finally {
    isSubmitting.value = false
  }
}

async function handleRegister() {
  errorMessage.value = ''

  try {
    validateIdentityStep()
    isSubmitting.value = true

    await registerMember({
      loginName: form.loginName.trim(),
      password: form.password,
      phoneNumber: form.phoneNumber.trim(),
      name: form.name.trim(),
      idCard: form.idCard.trim().toUpperCase(),
    })

    await router.push({ name: 'login' })
  } catch (error) {
    errorMessage.value = error instanceof ApiError || error instanceof Error ? error.message : '注册失败，请稍后重试。'
  } finally {
    isSubmitting.value = false
  }
}

function goBackToAccountStep() {
  errorMessage.value = ''
  currentStep.value = 'account'
}
</script>

<template>
  <main class="register-page">
    <section class="register-card">
      <p class="eyebrow">TJ-GYM · Member</p>
      <h1>会员注册</h1>
      <p class="intro">当前仅开放会员自助注册。请先完成账号信息校验，再进行实名认证；注册完成后返回登录页。</p>

      <div class="step-indicator" aria-label="注册步骤">
        <div :class="['step-chip', { active: currentStep === 'account' }]">1. 账号信息</div>
        <div :class="['step-chip', { active: currentStep === 'identity' }]">2. 实名认证</div>
      </div>

      <form
        class="register-form"
        @submit.prevent="currentStep === 'account' ? goToIdentityStep() : handleRegister()"
      >
        <div v-if="currentStep === 'account'" class="step-panel">
          <label class="field">
            <span>登录名 *</span>
            <input v-model="form.loginName" type="text" autocomplete="username" placeholder="用于登录" />
          </label>
          <label class="field">
            <span>密码 *</span>
            <input
              v-model="form.password"
              type="password"
              autocomplete="new-password"
              placeholder="8 位以上，含大小写字母和数字"
            />
          </label>
          <label class="field">
            <span>确认密码 *</span>
            <input v-model="form.confirmPassword" type="password" autocomplete="new-password" />
          </label>
          <label class="field">
            <span>手机号 *</span>
            <input v-model="form.phoneNumber" type="tel" autocomplete="tel" placeholder="请输入 11 位手机号" />
          </label>
        </div>

        <div v-else class="step-panel">
          <label class="field">
            <span>姓名 *</span>
            <input v-model="form.name" type="text" autocomplete="name" placeholder="请输入真实姓名" />
          </label>
          <label class="field">
            <span>身份证号 *</span>
            <input v-model="form.idCard" type="text" inputmode="numeric" placeholder="请输入 18 位身份证号" />
          </label>
          <label class="field">
            <span>生日</span>
            <input :value="derivedBirthday" type="text" disabled placeholder="将根据身份证号自动带出" />
          </label>
          <label class="field">
            <span>性别</span>
            <input :value="derivedGender" type="text" disabled placeholder="将根据身份证号自动带出" />
          </label>
        </div>

        <p class="error-message" aria-live="polite">{{ errorMessage }}</p>

        <div class="action-row">
          <button
            v-if="currentStep === 'identity'"
            class="secondary-btn"
            type="button"
            :disabled="isSubmitting"
            @click="goBackToAccountStep"
          >
            上一步
          </button>
          <button class="submit-btn" type="submit" :disabled="isSubmitting">
            {{
              isSubmitting
                ? '提交中...'
                : currentStep === 'account'
                  ? '下一步'
                  : '完成注册并返回登录'
            }}
          </button>
        </div>
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
  min-height: 760px;
  padding: 40px 32px;
  border-radius: 24px;
  background: rgba(255, 255, 255, 0.96);
  color: #182336;
  box-shadow: 0 24px 60px rgba(0, 0, 0, 0.28);
  display: flex;
  flex-direction: column;
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

.step-indicator {
  display: flex;
  gap: 10px;
  margin-bottom: 22px;
}

.step-chip {
  flex: 1;
  border-radius: 999px;
  padding: 9px 14px;
  text-align: center;
  font-size: 0.9rem;
  font-weight: 600;
  color: #5b6b86;
  background: #eef3fb;
}

.step-chip.active {
  color: #fff;
  background: #2c57d2;
}

.register-form {
  display: grid;
  gap: 14px;
  flex: 1;
}

.step-panel {
  display: grid;
  gap: 14px;
  align-content: start;
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

.field input:disabled {
  color: #5b6b86;
  background: #f6f8fc;
}

.error-message {
  margin: 0;
  color: #c0392b;
  font-size: 0.92rem;
  min-height: 1.4em;
}

.action-row {
  display: flex;
  gap: 12px;
  margin-top: auto;
}

.submit-btn,
.secondary-btn {
  border: 0;
  border-radius: 12px;
  padding: 14px 18px;
  font: inherit;
  font-weight: 700;
}

.submit-btn {
  flex: 1;
  background: #16233b;
  color: #fff;
  cursor: pointer;
}

.secondary-btn {
  min-width: 110px;
  background: #eef3fb;
  color: #23334f;
  cursor: pointer;
}

.submit-btn:disabled,
.secondary-btn:disabled {
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
