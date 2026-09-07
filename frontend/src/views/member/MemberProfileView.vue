<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { cancelMember, getMemberProfile, updateMember, type MemberProfile } from '@/api/members'
import { useAuthStore } from '@/stores/auth'
import StateCard from '@/components/ui/StateCard.vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const profile = ref<MemberProfile | null>(null)
const loading = ref(true)
const errorMessage = ref('')
const isEditing = ref(false)
const isSaving = ref(false)
const isCancelling = ref(false)
const saveNotice = ref('')

const draft = reactive({
  displayName: '',
  phoneNumber: '',
})

const memberId = computed(() => Number(route.params.id))

function formatDate(value?: string) {
  if (!value) {
    return '未填写'
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return '未填写'
  }

  return date.toLocaleDateString('zh-CN')
}

function maskIdCard(value?: string) {
  if (!value) {
    return '未填写'
  }

  if (value.length <= 8) {
    return value
  }

  return `${value.slice(0, 4)} ******** ${value.slice(-4)}`
}

function resolveGenderLabel(value?: string) {
  if (!value) {
    return '未填写'
  }

  if (value === 'M') {
    return '男'
  }

  if (value === 'F') {
    return '女'
  }

  return value
}

function fillDraft(source: MemberProfile) {
  draft.displayName = source.name
  draft.phoneNumber = source.phoneNumber ?? ''
}

async function loadProfile() {
  loading.value = true
  errorMessage.value = ''

  try {
    profile.value = await getMemberProfile(memberId.value)
    if (profile.value) {
      fillDraft(profile.value)
    }
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '会员档案加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

function startEditing() {
  if (!profile.value) {
    return
  }

  fillDraft(profile.value)
  saveNotice.value = ''
  isEditing.value = true
}

function cancelEditing() {
  if (profile.value) {
    fillDraft(profile.value)
  }
  saveNotice.value = ''
  isEditing.value = false
}

async function saveProfile() {
  if (!profile.value) {
    return
  }

  if (!draft.displayName.trim()) {
    saveNotice.value = '昵称不能为空。'
    return
  }

  isSaving.value = true
  saveNotice.value = ''
  errorMessage.value = ''

  try {
    profile.value = await updateMember(memberId.value, {
      name: draft.displayName.trim(),
      phoneNumber: draft.phoneNumber.trim() || undefined,
    })
    fillDraft(profile.value)
    isEditing.value = false
  } catch (error) {
    saveNotice.value = error instanceof ApiError ? error.message : '保存失败，请稍后重试。'
  } finally {
    isSaving.value = false
  }
}

function goToIdentityPage() {
  router.push(`/member/profile/${memberId.value}/edit`)
}

async function cancelAccount() {
  if (!profile.value) {
    return
  }

  if (!confirm('确定要注销当前会员账号吗？注销后将无法直接恢复。')) {
    return
  }

  isCancelling.value = true
  errorMessage.value = ''

  try {
    await cancelMember(profile.value.memberId)
    authStore.clearSession()
    await router.push('/login')
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '注销失败，请稍后重试。'
  } finally {
    isCancelling.value = false
  }
}

onMounted(loadProfile)
</script>

<template>
  <div class="member-profile">
    <div v-if="loading" class="loading-state" aria-live="polite">
      <span class="loading-spinner" aria-hidden="true"></span>
      <p>会员档案加载中...</p>
    </div>

    <StateCard v-else-if="errorMessage" :message="errorMessage" type="error" />

    <template v-else-if="profile">
      <div v-if="saveNotice" class="alert-banner">{{ saveNotice }}</div>

      <section class="profile-header">
        <div class="avatar">{{ profile.name?.slice(0, 1) || '会' }}</div>
        <div class="header-info">
          <h1>{{ profile.name }}</h1>
          <div class="tags">
            <span>已于 {{ formatDate(profile.registerDate) }} 成为会员</span>
            <span class="divider">·</span>
            <span :class="profile.idCard ? 'text-emerald' : 'text-gray'">
              {{ profile.idCard ? '已实名认证' : '未实名认证' }}
            </span>
          </div>
        </div>
      </section>

      <div class="content-layout">
        <article class="flat-card">
          <div class="card-header">
            <h2>基础信息</h2>
            <div class="actions">
              <button v-if="!isEditing" type="button" class="btn-primary" @click="startEditing">
                编辑资料
              </button>
              <template v-else>
                <button type="button" class="btn-ghost" @click="cancelEditing" :disabled="isSaving">
                  取消
                </button>
                <button type="button" class="btn-primary" @click="saveProfile" :disabled="isSaving">
                  {{ isSaving ? '保存中...' : '保存' }}
                </button>
              </template>
            </div>
          </div>

          <div class="card-body">
            <div v-if="!isEditing" class="data-grid">
              <div class="data-item">
                <span>昵称</span>
                <strong>{{ profile.name || '未填写' }}</strong>
              </div>
              <div class="data-item">
                <span>手机号</span>
                <strong>{{ profile.phoneNumber || '未填写' }}</strong>
              </div>
              <div class="data-item">
                <span>会员编号</span>
                <strong>#{{ profile.memberId }}</strong>
              </div>
              <div class="data-item">
                <span>会员等级</span>
                <strong>{{ profile.memberLevel || '普通会员' }}</strong>
              </div>
            </div>

            <form v-else class="form-grid" @submit.prevent="saveProfile">
              <div class="form-item">
                <label>昵称</label>
                <input v-model="draft.displayName" type="text" placeholder="请输入昵称" maxlength="50" />
              </div>
              <div class="form-item">
                <label>手机号</label>
                <input v-model="draft.phoneNumber" type="tel" placeholder="请输入手机号" maxlength="20" />
              </div>
            </form>
          </div>
        </article>

        <article class="flat-card">
          <div class="card-header">
            <h2>实名认证</h2>
            <button type="button" class="btn-primary" @click="goToIdentityPage">
              前往修改
            </button>
          </div>

          <div class="card-body">
            <div class="data-grid">
              <div class="data-item full-width">
                <span>身份证号</span>
                <strong>{{ maskIdCard(profile.idCard) }}</strong>
              </div>
              <div class="data-item">
                <span>性别</span>
                <strong>{{ resolveGenderLabel(profile.gender) }}</strong>
              </div>
              <div class="data-item">
                <span>生日</span>
                <strong>{{ formatDate(profile.birthday) }}</strong>
              </div>
            </div>
          </div>
        </article>
      </div>

      <div class="danger-zone">
        <button type="button" class="btn-danger-text" @click="cancelAccount" :disabled="isCancelling">
          {{ isCancelling ? '注销中...' : '注销会员' }}
        </button>
      </div>
    </template>
  </div>
</template>

<style scoped>
.member-profile {
  max-width: 1080px;
  margin: 0 auto;
  padding: 36px 32px 92px;
  display: flex;
  flex-direction: column;
  gap: 36px;
  font-family: ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 16px;
  padding: 88px 0 64px;
  color: #4b5563;
}

.loading-state p {
  margin: 0;
  font-size: 18px;
  font-weight: 500;
}

.loading-spinner {
  width: 34px;
  height: 34px;
  border: 3px solid #dbe7ff;
  border-top-color: #3b82f6;
  border-radius: 50%;
  animation: spin 0.85s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.alert-banner {
  padding: 14px 18px;
  background: #f8fbff;
  border: 1px solid #dbeafe;
  border-radius: 10px;
  color: #375174;
  font-size: 15px;
  font-weight: 500;
}

.profile-header {
  display: flex;
  align-items: center;
  gap: 28px;
  padding-bottom: 36px;
  border-bottom: 1px solid #e5e7eb;
}

.avatar {
  width: 84px;
  height: 84px;
  border-radius: 50%;
  background: #2563eb;
  color: #fff;
  font-size: 32px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.header-info h1 {
  font-size: 32px;
  font-weight: 700;
  color: #111827;
  margin: 0 0 10px 0;
  letter-spacing: -0.02em;
}

.tags {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  font-size: 16px;
  color: #6b7280;
}

.divider {
  color: #d1d5db;
}

.text-emerald {
  color: #059669;
}

.text-gray {
  color: #9ca3af;
}

.content-layout {
  display: flex;
  flex-direction: column;
  gap: 28px;
}

.flat-card {
  background: #ffffff;
  border: 1px solid #e5e7eb;
  border-radius: 14px;
  box-shadow: 0 2px 8px rgba(15, 23, 42, 0.05);
  overflow: hidden;
}

.card-header {
  padding: 24px 28px;
  border-bottom: 1px solid #f3f4f6;
  background: #f9fafb;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-header h2 {
  font-size: 19px;
  font-weight: 700;
  color: #111827;
  margin: 0;
}

.actions {
  display: flex;
  gap: 10px;
}

.card-body {
  padding: 32px 28px;
}

.data-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 32px 28px;
}

.data-item {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.data-item.full-width {
  grid-column: 1 / -1;
}

.data-item span {
  font-size: 13px;
  font-weight: 600;
  color: #6b7280;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.data-item strong {
  font-size: 18px;
  color: #111827;
  font-weight: 600;
  line-height: 1.45;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 28px;
}

.form-item {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.form-item label {
  font-size: 14px;
  font-weight: 600;
  color: #374151;
}

.form-item input {
  width: 100%;
  padding: 13px 16px;
  border: 1px solid #cfd8e3;
  border-radius: 10px;
  font-size: 17px;
  color: #111827;
  background: #ffffff;
  transition: all 0.2s;
  outline: none;
}

.form-item input:focus {
  border-color: #3b82f6;
  box-shadow: 0 0 0 1px #3b82f6;
}

.btn-primary,
.btn-ghost,
.btn-danger-text {
  padding: 10px 18px;
  font-size: 15px;
  font-weight: 600;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.2s;
  border: none;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.btn-primary {
  background: #2563eb;
  color: #ffffff;
}

.btn-primary:hover:not(:disabled) {
  background: #1d4ed8;
}

.btn-ghost {
  background: #eff6ff;
  color: #2563eb;
}

.btn-ghost:hover:not(:disabled) {
  background: #dbeafe;
  color: #1d4ed8;
}

.danger-zone {
  display: flex;
  justify-content: center;
  padding-top: 8px;
}

.btn-danger-text {
  background: transparent;
  color: #dc2626;
  padding: 12px 22px;
}

.btn-danger-text:hover:not(:disabled) {
  background: #fee2e2;
}

.btn-primary:disabled,
.btn-ghost:disabled,
.btn-danger-text:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

@media (max-width: 640px) {
  .member-profile {
    padding: 24px 18px 68px;
    gap: 28px;
  }

  .loading-state {
    padding: 64px 0 44px;
  }

  .data-grid,
  .form-grid {
    grid-template-columns: 1fr;
    gap: 22px;
  }

  .profile-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 18px;
    padding-bottom: 28px;
  }

  .avatar {
    width: 72px;
    height: 72px;
    font-size: 28px;
  }

  .header-info h1 {
    font-size: 28px;
  }

  .tags {
    font-size: 15px;
  }

  .card-header,
  .card-body {
    padding-left: 22px;
    padding-right: 22px;
  }

  .card-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 14px;
  }

  .actions {
    width: 100%;
    flex-wrap: wrap;
  }
}
</style>
