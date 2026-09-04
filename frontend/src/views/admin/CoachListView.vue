<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ApiError } from '@/api/http'
import {
  createCoach,
  deactivateCoach,
  getCoachManagementList,
  updateCoach,
  type CoachManagementListItem,
} from '@/api/coaches'
import StateCard from '@/components/ui/StateCard.vue'

const loading = ref(true)
const submitting = ref(false)
const errorMessage = ref('')
const dialogErrorMessage = ref('')
const coaches = ref<CoachManagementListItem[]>([])
const detailCoachId = ref<number | null>(null)
const editingCoachId = ref<number | null>(null)
const dialogMode = ref<'create' | 'edit' | null>(null)

const filters = reactive({
  keyword: '',
  sortBy: 'coachId' as 'coachId' | 'userId' | 'displayName' | 'coachName' | 'hireDate',
  sortDirection: 'desc' as 'asc' | 'desc',
  status: 'all' as 'all' | 'active' | 'inactive',
})

const form = reactive({
  loginName: '',
  password: '',
  displayName: '',
  coachName: '',
  phoneNumber: '',
  sex: '',
  specialty: '',
  coachSummary: '',
})

function normalizeSex(value: string) {
  return value === '男' || value === '女' ? value : ''
}

const coachSummaryMaxLength = 300

const sortOptions = [
  { label: '教练编号', value: 'coachId' },
  { label: '用户编号', value: 'userId' },
  { label: '昵称', value: 'displayName' },
  { label: '教练姓名', value: 'coachName' },
  { label: '入职时间', value: 'hireDate' },
]

const visibleCoaches = computed(() => {
  if (filters.status === 'active') {
    return coaches.value.filter(item => normalizeCoachStatus(item.status) === '在职')
  }
  if (filters.status === 'inactive') {
    return coaches.value.filter(item => normalizeCoachStatus(item.status) === '离职')
  }
  return coaches.value
})

const detailCoach = computed(() => coaches.value.find(item => item.coachId === detailCoachId.value) ?? null)
const isDialogOpen = computed(() => dialogMode.value !== null)
const dialogTitle = computed(() => (dialogMode.value === 'edit' ? '编辑教练' : '添加教练'))
const submitButtonText = computed(() => {
  if (submitting.value) {
    return dialogMode.value === 'edit' ? '保存中...' : '创建中...'
  }

  return dialogMode.value === 'edit' ? '保存修改' : '确认添加'
})
function formatDate(value?: string) {
  if (!value) return '未填写'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return '未填写'
  }

  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')
  return `${year}-${month}-${day}`
}

function toInputDate(value?: string) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return ''
  }

  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')
  return `${year}-${month}-${day}`
}

function normalizeCoachStatus(value?: string) {
  return value === '离职' ? '离职' : '在职'
}

function resolveStatusLabel(value?: string) {
  return normalizeCoachStatus(value)
}

function resolveBadgeTone(value?: string) {
  return normalizeCoachStatus(value) === '离职' ? 'is-inactive' : 'is-active'
}

function resetForm() {
  form.loginName = ''
  form.password = ''
  form.displayName = ''
  form.coachName = ''
  form.phoneNumber = ''
  form.sex = ''
  form.specialty = ''
  form.coachSummary = ''
}

function fillForm(coach?: CoachManagementListItem | null) {
  if (!coach) {
    resetForm()
    return
  }

  form.loginName = coach.loginName ?? ''
  form.password = ''
  form.displayName = coach.displayName ?? ''
  form.coachName = coach.coachName ?? ''
  form.phoneNumber = coach.phoneNumber ?? ''
  form.sex = coach.sex ?? ''
  form.specialty = coach.specialty ?? ''
  form.coachSummary = coach.coachSummary ?? ''
}

async function loadCoaches() {
  loading.value = true
  errorMessage.value = ''

  try {
    coaches.value = await getCoachManagementList({
      keyword: filters.keyword.trim() || undefined,
      sortBy: filters.sortBy,
      sortDirection: filters.sortDirection,
      status: filters.status,
    })
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '教练列表加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

function searchCoaches() {
  loadCoaches()
}

function resetFilters() {
  filters.keyword = ''
  filters.sortBy = 'coachId'
  filters.sortDirection = 'desc'
  filters.status = 'all'
  loadCoaches()
}

function toggleSortDirection() {
  filters.sortDirection = filters.sortDirection === 'asc' ? 'desc' : 'asc'
  loadCoaches()
}

function openDetail(coachId: number) {
  detailCoachId.value = coachId
}

function closeDetail() {
  detailCoachId.value = null
}

function openCreateDialog() {
  dialogMode.value = 'create'
  editingCoachId.value = null
  dialogErrorMessage.value = ''
  fillForm()
}

function openEditDialog(coach: CoachManagementListItem) {
  dialogMode.value = 'edit'
  detailCoachId.value = coach.coachId
  editingCoachId.value = coach.coachId
  dialogErrorMessage.value = ''
  fillForm(coach)
}

function closeDialog() {
  dialogMode.value = null
  editingCoachId.value = null
  dialogErrorMessage.value = ''
  resetForm()
}

async function handleDeactivate(coach: CoachManagementListItem) {
  if (submitting.value || normalizeCoachStatus(coach.status) === '离职') {
    return
  }

  const confirmed = window.confirm(`确认注销教练“${coach.displayName || coach.coachName}”吗？该操作会同步注销账号并将教练状态改为离职。`)
  if (!confirmed) {
    return
  }

  submitting.value = true
  errorMessage.value = ''

  try {
    await deactivateCoach(coach.coachId)
    if (detailCoachId.value === coach.coachId) {
      closeDetail()
    }
    if (editingCoachId.value === coach.coachId) {
      closeDialog()
    }
    await loadCoaches()
  } catch (error) {
    errorMessage.value = error instanceof ApiError || error instanceof Error ? error.message : '注销失败，请稍后重试。'
  } finally {
    submitting.value = false
  }
}

async function handleSubmit() {
  const isEditMode = dialogMode.value === 'edit'
  dialogErrorMessage.value = ''
  const coachId = editingCoachId.value

  if (isEditMode && coachId === null) {
    dialogErrorMessage.value = '未找到要编辑的教练。'
    return
  }

  try {
    submitting.value = true

    if (isEditMode) {
      dialogMode.value = null
    }

    const loginName = form.loginName.trim()
    const displayName = form.displayName.trim()
    const coachName = form.coachName.trim()
    const password = form.password.trim()
    const phoneNumber = form.phoneNumber.trim()
    const sex = normalizeSex(form.sex)
    if (!sex) {
      throw new Error('请选择性别。')
    }
    const specialty = form.specialty.trim()
    const coachSummary = form.coachSummary.trim()

    if (!displayName) {
      throw new Error('请输入昵称。')
    }
    if (!coachName) {
      throw new Error('请输入教练姓名。')
    }
    if (dialogMode.value === 'create' && !loginName) {
      throw new Error('请输入登录名。')
    }

    if (coachSummary.length > coachSummaryMaxLength) {
      throw new Error(`教练简介不能超过 ${coachSummaryMaxLength} 个字符。`)
    }

    if (dialogMode.value === 'create') {
      if (!password) {
        throw new Error('请输入初始密码。')
      }

      await createCoach({
        loginName,
        password,
        displayName,
        coachName,
        phoneNumber: phoneNumber || undefined,
        sex,
        specialty: specialty || undefined,
        coachSummary: coachSummary || undefined,
      })
    } else {
      await updateCoach(coachId!, {
        displayName,
        coachName,
        phoneNumber: phoneNumber || undefined,
        sex,
        specialty: specialty || undefined,
        coachSummary: coachSummary || undefined,
      })
    }

    await loadCoaches()
    closeDialog()
  } catch (error) {
    if (isEditMode) {
      dialogMode.value = 'edit'
    }
    dialogErrorMessage.value = error instanceof ApiError || error instanceof Error ? error.message : '保存失败，请稍后重试。'
  } finally {
    submitting.value = false
  }
}

onMounted(loadCoaches)
</script>

<template>
  <div class="coaches-view">
    <section class="page-head">
      <div>
        <p class="eyebrow">管理员端</p>
        <h1>教练管理</h1>
      </div>
      <div class="head-actions">
        <button type="button" class="btn-ghost" @click="resetFilters">重置</button>
        <button type="button" class="btn-primary" @click="loadCoaches">重新加载</button>
        <button type="button" class="btn-primary btn-add" @click="openCreateDialog">添加教练</button>
      </div>
    </section>

    <section class="filter-bar">
      <div class="search-group">
        <input v-model="filters.keyword" class="search-input" type="text" placeholder="搜索 CoachID / UserID / 姓名 / 手机号 / 专长" @keyup.enter="searchCoaches" />
        <button type="button" class="btn-primary search-btn" @click="searchCoaches">搜索</button>
      </div>
      <div class="control-group">
        <select v-model="filters.status" class="select-input compact-select" @change="loadCoaches">
          <option value="all">全部状态</option>
          <option value="active">在职</option>
          <option value="inactive">离职</option>
        </select>
        <select v-model="filters.sortBy" class="select-input compact-select" @change="loadCoaches">
          <option v-for="item in sortOptions" :key="item.value" :value="item.value">按{{ item.label }}排序</option>
        </select>
        <button type="button" class="btn-ghost" @click="toggleSortDirection">
          {{ filters.sortDirection === 'asc' ? '升序' : '降序' }}
        </button>
      </div>
    </section>

    <StateCard v-if="errorMessage" :message="errorMessage" type="error" />
    <div v-else-if="loading" class="loading-state">加载中...</div>

    <section v-else class="table-card">
      <div class="table-head">
        <span>共 {{ visibleCoaches.length }} 条</span>
      </div>

      <div v-if="!visibleCoaches.length" class="empty-state">暂无教练数据</div>

      <div v-else class="coach-list">
        <article v-for="coach in visibleCoaches" :key="coach.coachId" class="coach-row">
          <div class="avatar">{{ coach.coachName?.slice(0, 1) || 'C' }}</div>
          <div class="main-info">
            <strong>{{ coach.displayName || coach.coachName || '未命名教练' }}</strong>
            <span>#{{ coach.coachId }}</span>
          </div>
          <div class="row-actions">
            <span class="status-pill" :class="resolveBadgeTone(coach.status)">{{ resolveStatusLabel(coach.status) }}</span>
            <button type="button" class="btn-ghost" @click="openDetail(coach.coachId)">详情</button>
            <button type="button" class="btn-ghost" @click="openEditDialog(coach)">编辑</button>
            <button type="button" class="btn-danger-soft" :disabled="submitting || normalizeCoachStatus(coach.status) === '离职'" @click="handleDeactivate(coach)">
              注销
            </button>
          </div>
        </article>
      </div>
    </section>

    <div v-if="detailCoach" class="detail-mask" @click.self="closeDetail">
      <section class="detail-popup">
        <div class="detail-popup-head">
          <div>
            <p class="eyebrow">教练详情</p>
            <h2>{{ detailCoach.displayName }}</h2>
            <p>教练编号 #{{ detailCoach.coachId }}</p>
          </div>
          <button type="button" class="btn-ghost" @click="closeDetail">关闭</button>
        </div>

        <div class="detail-grid">
          <div class="detail-item"><span class="detail-label">用户编号</span><strong class="detail-value">#{{ detailCoach.userId }}</strong></div>
          <div class="detail-item"><span class="detail-label">登录名</span><strong class="detail-value">{{ detailCoach.loginName || '未填写' }}</strong></div>
          <div class="detail-item"><span class="detail-label">教练姓名</span><strong class="detail-value">{{ detailCoach.coachName }}</strong></div>
          <div class="detail-item"><span class="detail-label">手机号</span><strong class="detail-value">{{ detailCoach.phoneNumber || '未填写' }}</strong></div>
          <div class="detail-item"><span class="detail-label">性别</span><strong class="detail-value">{{ detailCoach.sex || '未填写' }}</strong></div>
          <div class="detail-item"><span class="detail-label">专长</span><strong class="detail-value">{{ detailCoach.specialty || '未填写' }}</strong></div>
          <div class="detail-item"><span class="detail-label">入职时间</span><strong class="detail-value">{{ formatDate(detailCoach.hireDate) }}</strong></div>
          <div class="detail-item full-width"><span class="detail-label">状态</span><strong class="detail-value status-text" :class="resolveBadgeTone(detailCoach.status)">{{ resolveStatusLabel(detailCoach.status) }}</strong></div>
          <div class="detail-item full-width"><span class="detail-label">简介</span><strong class="detail-value multiline">{{ detailCoach.coachSummary || '未填写' }}</strong></div>
        </div>
      </section>
    </div>

    <div v-if="isDialogOpen" class="detail-mask" @click.self="closeDialog">
      <section class="dialog-card">
        <div class="detail-popup-head dialog-head">
          <div>
            <p class="eyebrow">管理员端</p>
            <h2>{{ dialogTitle }}</h2>
          </div>
          <button type="button" class="btn-ghost" :disabled="submitting" @click="closeDialog">关闭</button>
        </div>

        <form class="dialog-form" @submit.prevent="handleSubmit">
          <section class="form-section">
            <div class="section-head">
              <h3>基础账户信息</h3>
            </div>
            <div class="form-grid">
              <label v-if="dialogMode === 'edit'" class="field">
                <span>UserId</span>
                <input :value="detailCoach?.userId ?? ''" type="text" disabled />
              </label>
              <label v-if="dialogMode === 'edit'" class="field">
                <span>CoachId</span>
                <input :value="editingCoachId ?? ''" type="text" disabled />
              </label>
              <label v-if="dialogMode === 'create'" class="field">
                <span>登录名 *</span>
                <input v-model="form.loginName" type="text" placeholder="请输入登录名" />
              </label>
              <label class="field">
                <span>{{ dialogMode === 'create' ? '初始密码 *' : '昵称 *' }}</span>
                <input
                  v-if="dialogMode === 'create'"
                  v-model="form.password"
                  type="password"
                  placeholder="至少 8 位，含大小写字母和数字"
                />
                <input v-else v-model="form.displayName" type="text" placeholder="请输入昵称" />
              </label>
              <label v-if="dialogMode === 'create'" class="field">
                <span>昵称 *</span>
                <input v-model="form.displayName" type="text" placeholder="请输入昵称" />
              </label>
              <label class="field">
                <span>手机号</span>
                <input v-model="form.phoneNumber" type="text" inputmode="numeric" placeholder="请输入 11 位手机号" />
              </label>
            </div>
          </section>

          <section class="form-section">
            <div class="section-head">
              <h3>教练业务信息</h3>
            </div>
            <div class="form-grid">
              <label class="field">
                <span>教练姓名 *</span>
                <input v-model="form.coachName" type="text" placeholder="请输入教练姓名" />
              </label>
              <label v-if="dialogMode === 'edit'" class="field">
                <span>入职日期</span>
                <input :value="formatDate(detailCoach?.hireDate)" type="text" disabled />
              </label>
              <label class="field">
                <span>性别 *</span>
                <select v-model="form.sex" class="select-input">
                  <option disabled value="">请选择</option>
                  <option value="男">男</option>
                  <option value="女">女</option>
                </select>
              </label>
              <label class="field">
                <span>专长</span>
                <input v-model="form.specialty" type="text" placeholder="例如：力量训练 / 瑜伽" />
              </label>
              <label class="field field-full">
                <span>教练简介</span>
                <textarea v-model="form.coachSummary" :maxlength="coachSummaryMaxLength" rows="4" placeholder="请输入教练简介"></textarea>
                <small class="field-hint">{{ form.coachSummary.length }}/{{ coachSummaryMaxLength }}</small>
              </label>
            </div>
          </section>

          <p class="error-message" aria-live="polite">{{ dialogErrorMessage }}</p>

          <div class="action-row">
            <button class="secondary-btn" type="button" :disabled="submitting" @click="closeDialog">取消</button>
            <button class="submit-btn" type="submit" :disabled="submitting">{{ submitButtonText }}</button>
          </div>
        </form>
      </section>
    </div>
  </div>
</template>

<style scoped>
.coaches-view {
  display: grid;
  gap: 18px;
}

.page-head,
.filter-bar,
.table-card {
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 14px;
  padding: 18px;
}

.page-head {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: center;
}

.eyebrow,
.table-head,
.main-info span {
  color: #6b7280;
}

.page-head h1,
.detail-popup h2,
.dialog-card h2 {
  margin: 4px 0;
}

.head-actions,
.control-group,
.search-group,
.row-actions,
.action-row {
  display: flex;
  gap: 10px;
  align-items: center;
}

.filter-bar {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;
}

.search-group {
  flex: 1;
  min-width: 280px;
}

.search-input,
.select-input,
.field input,
.field textarea {
  border: 1px solid #dbe3f0;
  border-radius: 10px;
  padding: 10px 12px;
}

.search-input {
  flex: 1;
}

.coach-list {
  display: grid;
  gap: 12px;
  margin-top: 12px;
}

.coach-row {
  display: grid;
  grid-template-columns: 44px minmax(0, 1fr) auto;
  gap: 18px;
  align-items: center;
  padding: 14px 16px;
  border: 1px solid #eef2f7;
  border-radius: 12px;
}

.coach-row:nth-child(odd) {
  background: #f8fafc;
}

.coach-row:nth-child(even) {
  background: #ffffff;
}

.avatar {
  width: 44px;
  height: 44px;
  border-radius: 999px;
  display: grid;
  place-items: center;
  background: #dbeafe;
  color: #1d4ed8;
  font-weight: 700;
}

.main-info {
  display: grid;
  gap: 6px;
  margin-left: 4px;
}

.main-info strong {
  font-size: 16px;
  color: #111827;
}

.row-actions {
  flex-wrap: wrap;
  justify-content: flex-end;
}

.status-pill { padding: 4px 10px; border-radius: 999px; font-size: 12px; font-weight: 600; white-space: nowrap; }
.status-is-active, .is-active { background: #e8f7ef; color: #137333; }
.is-inactive { background: #fee2e2; color: #b91c1c; }
.btn-ghost, .btn-primary, .btn-danger-soft, .submit-btn, .secondary-btn { border: none; border-radius: 10px; padding: 10px 14px; cursor: pointer; }
.btn-ghost, .secondary-btn { background: #eff6ff; color: #2563eb; }
.btn-primary, .submit-btn { background: #2563eb; color: #fff; }
.btn-danger-soft { background: #fff1f2; color: #dc2626; }
.btn-ghost:disabled, .btn-danger-soft:disabled, .submit-btn:disabled, .secondary-btn:disabled { opacity: 0.72; cursor: not-allowed; }
.detail-mask { position: fixed; inset: 0; background: rgba(15, 23, 42, 0.45); display: grid; place-items: center; padding: 20px; z-index: 20; }
.detail-popup, .dialog-card { width: min(760px, 100%); background: #fff; border-radius: 16px; padding: 20px; box-shadow: 0 24px 60px rgba(15, 23, 42, 0.18); }
.dialog-card { width: min(920px, 100%); }
.detail-popup-head { display: flex; justify-content: space-between; gap: 12px; align-items: flex-start; padding-bottom: 16px; border-bottom: 1px solid #f1f5f9; }
.detail-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; margin-top: 16px; }
.detail-item { display: grid; gap: 10px; padding: 14px 16px; border-radius: 14px; background: #f8fbff; border: 1px solid #e6edf8; }
.detail-label { font-size: 12px; font-weight: 600; letter-spacing: 0.04em; text-transform: uppercase; color: #64748b; }
.detail-value { color: #0f172a; font-size: 15px; font-weight: 700; line-height: 1.5; }
.detail-value.multiline { white-space: pre-wrap; overflow-wrap: anywhere; word-break: break-word; }
.detail-value.status-text { display: inline-flex; align-items: center; width: fit-content; padding: 6px 10px; border-radius: 999px; font-size: 13px; }
.detail-item.full-width { grid-column: 1 / -1; }
.dialog-form { display: grid; gap: 18px; margin-top: 18px; }
.form-section { display: grid; gap: 16px; padding: 18px; border: 1px solid #e6edf8; border-radius: 14px; background: #f8fbff; }
.section-head { display: grid; gap: 4px; }
.section-head h3, .section-head p { margin: 0; }
.section-head p { color: #6b7280; font-size: 14px; }
.form-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
.field { display: grid; gap: 8px; }
.field span { font-size: 14px; color: #475569; font-weight: 600; }
.field input, .field textarea, .field .select-input { width: 100%; background: #fff; }
.field input:disabled { color: #64748b; background: #f8fafc; cursor: not-allowed; }
.field textarea { resize: vertical; min-height: 108px; }
.field-hint { color: #6b7280; font-size: 12px; justify-self: end; }
.field-full { grid-column: 1 / -1; }
.error-message { margin: 0; color: #dc2626; min-height: 1.4em; font-size: 14px; }
.loading-state, .empty-state { padding: 28px; text-align: center; color: #6b7280; }
.action-row { justify-content: flex-end; }
@media (max-width: 960px) {
  .page-head,
  .filter-bar,
  .detail-grid,
  .form-grid {
    grid-template-columns: 1fr;
  }

  .page-head,
  .detail-popup-head {
    flex-direction: column;
    align-items: flex-start;
  }

  .coach-row {
    grid-template-columns: 1fr;
  }

  .row-actions {
    justify-content: flex-start;
  }
}
</style>
