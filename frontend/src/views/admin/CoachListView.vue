<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ApiError } from '@/api/http'
import { getCoachDetail, getCoachManagementList, type CoachManagementListItem } from '@/api/coaches'
import StateCard from '@/components/ui/StateCard.vue'

const loading = ref(true)
const errorMessage = ref('')
const coaches = ref<CoachManagementListItem[]>([])
const detailCoachId = ref<number | null>(null)

const filters = reactive({
  keyword: '',
  sortBy: 'coachId' as 'coachId' | 'userId' | 'displayName' | 'coachName' | 'hireDate',
  sortDirection: 'desc' as 'asc' | 'desc',
  status: 'all' as 'all' | 'active' | 'inactive',
})

const sortOptions = [
  { label: '教练编号', value: 'coachId' },
  { label: '用户编号', value: 'userId' },
  { label: '昵称', value: 'displayName' },
  { label: '教练姓名', value: 'coachName' },
  { label: '入职时间', value: 'hireDate' },
]

const visibleCoaches = computed(() => {
  if (filters.status === 'active') {
    return coaches.value.filter(item => item.status !== '0')
  }
  if (filters.status === 'inactive') {
    return coaches.value.filter(item => item.status === '0')
  }
  return coaches.value
})

const detailCoach = computed(() => coaches.value.find(item => item.coachId === detailCoachId.value) ?? null)

function formatDate(value?: string) {
  if (!value) return '未填写'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '未填写' : date.toLocaleDateString('zh-CN')
}

function resolveStatusLabel(value?: string) {
  if (value === '1') return '在职'
  if (value === '0') return '离职'
  return value || '未填写'
}

function resolveBadgeTone(value?: string) {
  return value === '0' ? 'is-inactive' : 'is-active'
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

onMounted(loadCoaches)
</script>

<template>
  <div class="coaches-view">
    <section class="page-head">
      <div>
        <p class="eyebrow">管理员端</p>
        <h1>教练管理</h1>
        <p class="subtext">极简列表、搜索、排序、状态筛选、详情弹窗与添加按钮占位。</p>
      </div>
      <div class="head-actions">
        <button type="button" class="btn-ghost" @click="resetFilters">重置</button>
        <button type="button" class="btn-primary" @click="loadCoaches">重新加载</button>
        <button type="button" class="btn-primary btn-add" disabled>添加教练（占位）</button>
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
            <span class="coach-id">CoachID #{{ coach.coachId }}</span>
            <strong>{{ coach.coachName || '未命名教练' }}</strong>
          </div>
          <div class="row-actions">
            <button type="button" class="btn-ghost" @click="openDetail(coach.coachId)">详情</button>
            <button type="button" class="btn-ghost" disabled>编辑</button>
            <button type="button" class="btn-danger-soft" disabled>注销</button>
            <span class="status-pill" :class="resolveBadgeTone(coach.status)">{{ resolveStatusLabel(coach.status) }}</span>
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
          <div class="detail-item"><span class="detail-label">教练姓名</span><strong class="detail-value">{{ detailCoach.coachName }}</strong></div>
          <div class="detail-item"><span class="detail-label">手机号</span><strong class="detail-value">{{ detailCoach.phoneNumber || '未填写' }}</strong></div>
          <div class="detail-item"><span class="detail-label">性别</span><strong class="detail-value">{{ detailCoach.sex || '未填写' }}</strong></div>
          <div class="detail-item"><span class="detail-label">专长</span><strong class="detail-value">{{ detailCoach.specialty || '未填写' }}</strong></div>
          <div class="detail-item"><span class="detail-label">入职时间</span><strong class="detail-value">{{ formatDate(detailCoach.hireDate) }}</strong></div>
          <div class="detail-item"><span class="detail-label">简介</span><strong class="detail-value multiline">{{ detailCoach.coachSummary || '未填写' }}</strong></div>
          <div class="detail-item"><span class="detail-label">状态</span><strong class="detail-value status-text" :class="resolveBadgeTone(detailCoach.status)">{{ resolveStatusLabel(detailCoach.status) }}</strong></div>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.coaches-view { display: grid; gap: 18px; }
.page-head, .filter-bar, .table-card { background: #fff; border: 1px solid #e5e7eb; border-radius: 14px; padding: 18px; }
.page-head { display: flex; justify-content: space-between; gap: 16px; align-items: center; }
.eyebrow, .subtext, .table-head, .main-info span { color: #6b7280; }
.page-head h1, .detail-popup h2 { margin: 4px 0; }
.head-actions, .control-group, .search-group, .row-actions { display: flex; gap: 10px; align-items: center; }
.filter-bar { display: flex; justify-content: space-between; gap: 16px; flex-wrap: wrap; }
.search-group { flex: 1; min-width: 280px; }
.search-input, .select-input { border: 1px solid #dbe3f0; border-radius: 10px; padding: 10px 12px; }
.search-input { flex: 1; }
.coach-list { display: grid; gap: 12px; margin-top: 14px; }
.coach-row { display: grid; grid-template-columns: 44px minmax(0, 1fr) auto; gap: 14px; align-items: center; padding: 14px 16px; border-radius: 14px; background: linear-gradient(180deg, #fbfdff 0%, #f4f8ff 100%); border: 1px solid #dce8fb; box-shadow: 0 10px 24px rgba(37, 99, 235, 0.06); }
.avatar { width: 44px; height: 44px; border-radius: 50%; display: grid; place-items: center; background: #e8f0ff; color: #285cff; font-weight: 700; }
.main-info { display: grid; gap: 6px; }
.coach-id { font-size: 12px; letter-spacing: 0.02em; text-transform: uppercase; }
.main-info strong { font-size: 17px; color: #111827; }
.row-actions { flex-wrap: wrap; justify-content: flex-end; }
.status-pill { padding: 4px 10px; border-radius: 999px; font-size: 12px; font-weight: 600; white-space: nowrap; }
.status-is-active, .is-active { background: #e8f7ef; color: #137333; }
.is-inactive { background: #fee2e2; color: #b91c1c; }
.btn-ghost, .btn-primary, .btn-danger-soft { border: none; border-radius: 10px; padding: 10px 14px; cursor: pointer; }
.btn-ghost { background: #eff6ff; color: #2563eb; }
.btn-primary { background: #2563eb; color: #fff; }
.btn-danger-soft { background: #fff1f2; color: #dc2626; }
.btn-add:disabled, .btn-ghost:disabled, .btn-danger-soft:disabled { opacity: 0.72; cursor: not-allowed; }
.detail-mask { position: fixed; inset: 0; background: rgba(15, 23, 42, 0.45); display: grid; place-items: center; padding: 20px; z-index: 20; }
.detail-popup { width: min(760px, 100%); background: #fff; border-radius: 16px; padding: 20px; box-shadow: 0 24px 60px rgba(15, 23, 42, 0.18); }
.detail-popup-head { display: flex; justify-content: space-between; gap: 12px; align-items: flex-start; padding-bottom: 16px; border-bottom: 1px solid #f1f5f9; }
.detail-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; margin-top: 16px; }
.detail-item { display: grid; gap: 10px; padding: 14px 16px; border-radius: 14px; background: #f8fbff; border: 1px solid #e6edf8; }
.detail-label { font-size: 12px; font-weight: 600; letter-spacing: 0.04em; text-transform: uppercase; color: #64748b; }
.detail-value { color: #0f172a; font-size: 15px; font-weight: 700; line-height: 1.5; }
.detail-value.multiline { white-space: pre-wrap; }
.detail-value.status-text { display: inline-flex; align-items: center; width: fit-content; padding: 6px 10px; border-radius: 999px; font-size: 13px; }
.detail-item.full-width { grid-column: 1 / -1; }
.loading-state, .empty-state { padding: 28px; text-align: center; color: #6b7280; }
@media (max-width: 960px) { .page-head, .filter-bar, .detail-grid { grid-template-columns: 1fr; } .page-head, .detail-popup-head { flex-direction: column; align-items: flex-start; } .coach-row { grid-template-columns: 44px 1fr; } .row-actions { justify-content: flex-start; } }
</style>
