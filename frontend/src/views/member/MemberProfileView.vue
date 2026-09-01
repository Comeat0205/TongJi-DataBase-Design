<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { cancelMember, getMemberProfile, updateMember, type MemberProfile } from '@/api/members'
import { useAuthStore } from '@/stores/auth'
import PageHeader from '@/components/ui/PageHeader.vue'
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
  name: '',
  phoneNumber: '',
  gender: '',
  birthday: '',
  idCard: '',
})

const memberId = computed(() => Number(route.params.id))

function formatDate(value?: string) {
  if (!value) {
    return '未填写'
  }

  return new Date(value).toLocaleDateString('zh-CN')
}

function toDateInputValue(value?: string) {
  if (!value) {
    return ''
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return ''
  }

  return date.toISOString().slice(0, 10)
}

function fillDraft(source: MemberProfile) {
  draft.name = source.name
  draft.phoneNumber = source.phoneNumber ?? ''
  draft.gender = source.gender ?? ''
  draft.birthday = toDateInputValue(source.birthday)
  draft.idCard = source.idCard ?? ''
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

  if (!draft.name.trim()) {
    saveNotice.value = '姓名不能为空。'
    return
  }

  isSaving.value = true
  saveNotice.value = ''
  errorMessage.value = ''

  try {
    profile.value = await updateMember(memberId.value, {
      name: draft.name.trim(),
      phoneNumber: draft.phoneNumber.trim() || undefined,
      gender: draft.gender || undefined,
      birthday: draft.birthday || undefined,
      idCard: draft.idCard.trim() || undefined,
    })
    fillDraft(profile.value)
    isEditing.value = false
    saveNotice.value = '档案已保存。'
  } catch (error) {
    saveNotice.value = error instanceof ApiError ? error.message : '保存失败，请稍后重试。'
  } finally {
    isSaving.value = false
  }
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
    <PageHeader
      eyebrow="Member Profile"
      :title="isEditing ? '编辑会员资料' : '我的档案'"
      :subtitle="
        isEditing
          ? '修改后点击保存，将写入 MEMBER 表并刷新展示。'
          : '查看与编辑个人资料（功能点 #1 #2）。'
      "
    >
      <template #actions>
        <button v-if="!loading && !errorMessage && profile && !isEditing" type="button" class="primary-btn" @click="startEditing">
          编辑资料
        </button>
        <template v-else-if="isEditing">
          <button type="button" class="ghost-btn" @click="cancelEditing" :disabled="isSaving">取消</button>
          <button type="button" class="primary-btn" @click="saveProfile" :disabled="isSaving">
            {{ isSaving ? '保存中...' : '保存' }}
          </button>
        </template>
      </template>
    </PageHeader>

    <StateCard v-if="loading" message="会员档案加载中..." />
    <StateCard v-else-if="errorMessage" :message="errorMessage" type="error" />

    <template v-else-if="profile">
      <p v-if="saveNotice" class="save-notice">{{ saveNotice }}</p>

      <section v-if="!isEditing" class="profile-grid">
        <article class="profile-card summary-card">
          <p class="summary-label">会员编号</p>
          <h2>#{{ profile.memberId }}</h2>
          <p class="summary-name">{{ profile.name }}</p>
          <span class="summary-status">{{ profile.status || '状态未知' }}</span>
        </article>

        <article class="profile-card detail-card">
          <div class="detail-row">
            <span>手机号</span>
            <strong>{{ profile.phoneNumber || '未填写' }}</strong>
          </div>
          <div class="detail-row">
            <span>会员等级</span>
            <strong>{{ profile.memberLevel || '未设置' }}</strong>
          </div>
          <div class="detail-row">
            <span>性别</span>
            <strong>{{ profile.gender || '未填写' }}</strong>
          </div>
          <div class="detail-row">
            <span>生日</span>
            <strong>{{ formatDate(profile.birthday) }}</strong>
          </div>
          <div class="detail-row">
            <span>注册时间</span>
            <strong>{{ formatDate(profile.registerDate) }}</strong>
          </div>
          <div class="detail-row">
            <span>身份证号</span>
            <strong>{{ profile.idCard || '未填写' }}</strong>
          </div>
        </article>
      </section>

      <section v-else class="edit-layout">
        <article class="profile-card summary-card readonly-panel">
          <p class="summary-label">不可修改</p>
          <div class="readonly-list">
            <div class="readonly-row">
              <span>会员编号</span>
              <strong>#{{ profile.memberId }}</strong>
            </div>
            <div class="readonly-row">
              <span>会员等级</span>
              <strong>{{ profile.memberLevel || '未设置' }}</strong>
            </div>
            <div class="readonly-row">
              <span>注册时间</span>
              <strong>{{ formatDate(profile.registerDate) }}</strong>
            </div>
            <div class="readonly-row">
              <span>账户状态</span>
              <strong>{{ profile.status || '状态未知' }}</strong>
            </div>
          </div>
        </article>

        <form class="profile-card edit-form" @submit.prevent="saveProfile">
          <p class="form-eyebrow">可编辑字段 · 功能点 #1</p>
          <h2>基本资料</h2>

          <label class="field">
            <span>姓名</span>
            <input v-model="draft.name" type="text" placeholder="请输入姓名" />
          </label>

          <label class="field">
            <span>手机号</span>
            <input v-model="draft.phoneNumber" type="tel" placeholder="请输入手机号" />
          </label>

          <label class="field">
            <span>性别</span>
            <select v-model="draft.gender">
              <option value="">请选择</option>
              <option value="M">男</option>
              <option value="F">女</option>
            </select>
          </label>

          <label class="field">
            <span>生日</span>
            <input v-model="draft.birthday" type="date" />
          </label>

          <label class="field">
            <span>身份证号</span>
            <input v-model="draft.idCard" type="text" placeholder="请输入身份证号" />
          </label>

          <p class="form-hint">保存将调用 PUT /api/members/{id}，成功后刷新档案。</p>
        </form>
      </section>
    </template>

    <button
      v-if="profile && !loading && !errorMessage"
      type="button"
      class="cancel-fab"
      @click="cancelAccount"
      :disabled="isCancelling"
    >
      {{ isCancelling ? '注销中...' : '注销会员' }}
    </button>
  </div>
</template>

<style scoped>
.member-profile {
  display: grid;
  gap: 20px;
  padding-bottom: 88px;
}

.save-notice {
  margin: 0;
  padding: 12px 16px;
  border-radius: 12px;
  background: #fff7e6;
  color: #9a6700;
  font-size: 14px;
  line-height: 1.6;
}

.profile-grid,
.edit-layout {
  display: grid;
  grid-template-columns: 0.8fr 1.2fr;
  gap: 24px;
}

.profile-card {
  padding: 28px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.summary-card {
  display: flex;
  flex-direction: column;
  justify-content: center;
  background: linear-gradient(180deg, #ffffff 0%, #f6f9ff 100%);
}

.readonly-panel {
  justify-content: flex-start;
}

.summary-label,
.form-eyebrow {
  margin: 0;
  color: #72819a;
  font-size: 14px;
}

.summary-card h2,
.edit-form h2 {
  margin: 10px 0 0;
  font-size: 28px;
  color: #142239;
}

.summary-name {
  margin: 12px 0 0;
  font-size: 22px;
  font-weight: 600;
  color: #2a3c59;
}

.summary-status {
  margin-top: 18px;
  width: fit-content;
  padding: 8px 14px;
  border-radius: 999px;
  background: #e8f0ff;
  color: #2c57d2;
  font-size: 13px;
  font-weight: 600;
}

.detail-card,
.readonly-list,
.edit-form {
  display: grid;
  gap: 16px;
}

.detail-row,
.readonly-row {
  display: flex;
  justify-content: space-between;
  gap: 24px;
  padding-bottom: 14px;
  border-bottom: 1px solid #eef2f7;
}

.detail-row:last-child,
.readonly-row:last-child {
  border-bottom: none;
  padding-bottom: 0;
}

.detail-row span,
.readonly-row span,
.field span {
  color: #7a88a0;
}

.detail-row strong,
.readonly-row strong {
  color: #182337;
  font-weight: 600;
  text-align: right;
}

.field {
  display: grid;
  gap: 8px;
}

.field input,
.field select {
  width: 100%;
  padding: 12px 14px;
  border: 1px solid #d8e2f0;
  border-radius: 12px;
  background: #fff;
  color: #182337;
  font-size: 15px;
}

.field input:focus,
.field select:focus {
  outline: none;
  border-color: #4d77ff;
  box-shadow: 0 0 0 3px rgba(77, 119, 255, 0.12);
}

.form-hint {
  margin: 4px 0 0;
  color: #7a88a0;
  font-size: 13px;
  line-height: 1.6;
}

.primary-btn,
.ghost-btn,
.cancel-fab {
  border: none;
  border-radius: 12px;
  padding: 10px 16px;
  font-weight: 600;
  cursor: pointer;
}

.primary-btn {
  background: #285cff;
  color: #fff;
}

.ghost-btn {
  background: #eef2f7;
  color: #2a3c59;
}

.cancel-fab {
  position: fixed;
  right: 32px;
  bottom: 32px;
  padding: 10px 16px;
  border-radius: 12px;
  background: #d73c4f;
  color: #fff;
  font-size: 14px;
  line-height: 1;
  box-shadow: 0 12px 24px rgba(215, 60, 79, 0.25);
  z-index: 20;
}

@media (max-width: 900px) {
  .profile-grid,
  .edit-layout {
    grid-template-columns: 1fr;
  }

  .cancel-fab {
    right: 20px;
    bottom: 20px;
  }
}
</style>
