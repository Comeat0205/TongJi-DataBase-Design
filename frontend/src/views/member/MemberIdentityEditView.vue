<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { getMemberProfile, updateMember } from '@/api/members'

const idCardRegex = /^\d{17}[\dXx]$/
const idCardWeights = [7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2]
const idCardCheckCodes = ['1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2']

const route = useRoute()
const router = useRouter()

const memberId = computed(() => Number(route.params.id))
const isLoading = ref(true)
const isSubmitting = ref(false)
const errorMessage = ref('')

const form = reactive({
  name: '',
  idCard: '',
})

const originalIdentity = reactive({
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

function fillForm(name?: string, idCard?: string) {
  form.name = name ?? ''
  form.idCard = idCard ?? ''
}

function restoreOriginalIdentity() {
  fillForm(originalIdentity.name, originalIdentity.idCard)
}

async function loadProfile() {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const profile = await getMemberProfile(memberId.value)
    originalIdentity.name = profile.name ?? ''
    originalIdentity.idCard = profile.idCard ?? ''
    fillForm(profile.name, profile.idCard)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '实名认证信息加载失败，请稍后重试。'
  } finally {
    isLoading.value = false
  }
}

async function handleSubmit() {
  errorMessage.value = ''

  try {
    if (!form.name.trim()) {
      throw new Error('请输入真实姓名。')
    }

    const idCard = form.idCard.trim().toUpperCase()
    if (!idCard) {
      throw new Error('请输入身份证号。')
    }

    const parsed = parseIdentityCard(idCard)
    isSubmitting.value = true

    await updateMember(memberId.value, {
      name: form.name.trim(),
      idCard,
      gender: parsed.gender,
      birthday: parsed.birthday,
    })

    originalIdentity.name = form.name.trim()
    originalIdentity.idCard = idCard
    await router.push(`/member/profile/${memberId.value}`)
  } catch (error) {
    restoreOriginalIdentity()
    errorMessage.value = error instanceof ApiError || error instanceof Error ? error.message : '保存失败，请稍后重试。'
  } finally {
    isSubmitting.value = false
  }
}

function goBack() {
  restoreOriginalIdentity()
  router.push(`/member/profile/${memberId.value}`)
}

onMounted(loadProfile)
</script>

<template>
  <main class="identity-page">
    <section class="identity-card">
      <div class="card-head">
        <div>
          <p class="eyebrow">Member Identity</p>
          <h1>实名认证修改</h1>
        </div>
        <button type="button" class="back-btn" @click="goBack">返回档案</button>
      </div>

      <div v-if="isLoading" class="loading-state">
        <span class="loading-spinner" aria-hidden="true"></span>
        <p>实名认证信息加载中...</p>
      </div>

      <form v-else class="identity-form" @submit.prevent="handleSubmit">
        <div class="step-panel">
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
            <input :value="derivedBirthday" type="text" disabled placeholder="系统将依据身份证号自动解析" />
          </label>
          <label class="field">
            <span>性别</span>
            <input :value="derivedGender" type="text" disabled placeholder="系统将依据身份证号自动解析" />
          </label>
        </div>

        <p class="error-message" aria-live="polite">{{ errorMessage }}</p>

        <div class="action-row">
          <button class="secondary-btn" type="button" :disabled="isSubmitting" @click="goBack">
            取消
          </button>
          <button class="submit-btn" type="submit" :disabled="isSubmitting">
            {{ isSubmitting ? '保存中...' : '保存并返回档案' }}
          </button>
        </div>
      </form>
    </section>
  </main>
</template>

<style scoped>
.identity-page {
  min-height: 100%;
  display: grid;
  place-items: start center;
  padding: 40px 24px 72px;
}

.identity-card {
  width: min(860px, 100%);
  border: 1px solid #e5e7eb;
  border-radius: 18px;
  background: #fff;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.06);
  overflow: hidden;
}

.card-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding: 28px 32px 22px;
  border-bottom: 1px solid #f1f5f9;
  background: #f8fbff;
}

.eyebrow {
  margin: 0 0 8px;
  color: #2563eb;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

h1 {
  margin: 0;
  font-size: 30px;
  color: #111827;
}

.back-btn,
.submit-btn,
.secondary-btn {
  border: none;
  border-radius: 10px;
  padding: 12px 18px;
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
}

.back-btn,
.secondary-btn {
  background: #eff6ff;
  color: #2563eb;
}

.submit-btn {
  background: #2563eb;
  color: #fff;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 14px;
  padding: 72px 0;
  color: #4b5563;
}

.loading-state p {
  margin: 0;
  font-size: 17px;
}

.loading-spinner {
  width: 32px;
  height: 32px;
  border: 3px solid #dbeafe;
  border-top-color: #2563eb;
  border-radius: 50%;
  animation: spin 0.85s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.identity-form {
  display: grid;
  gap: 18px;
  padding: 28px 32px 32px;
}

.step-panel {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 22px;
}

.field {
  display: grid;
  gap: 8px;
}

.field span {
  font-size: 14px;
  color: #475569;
  font-weight: 600;
}

.field input {
  width: 100%;
  border: 1px solid #d5deec;
  border-radius: 12px;
  padding: 14px 16px;
  font-size: 16px;
  background: #fff;
}

.field input:disabled {
  color: #64748b;
  background: #f8fafc;
}

.field input:focus {
  outline: none;
  border-color: #2563eb;
  box-shadow: 0 0 0 1px #2563eb;
}

.error-message {
  margin: 0;
  color: #dc2626;
  min-height: 1.4em;
  font-size: 14px;
}

.action-row {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

.submit-btn:disabled,
.secondary-btn:disabled,
.back-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

@media (max-width: 720px) {
  .identity-page {
    padding: 24px 16px 48px;
  }

  .card-head,
  .identity-form {
    padding-left: 20px;
    padding-right: 20px;
  }

  .card-head {
    flex-direction: column;
  }

  .step-panel {
    grid-template-columns: 1fr;
  }

  h1 {
    font-size: 26px;
  }
}
</style>
