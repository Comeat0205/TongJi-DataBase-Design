<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { cancelMember, getMemberManagementList, type MemberManagementListItem } from '@/api/members'
import StateCard from '@/components/ui/StateCard.vue'

const router = useRouter()
const loading = ref(true)
const savingId = ref<number | null>(null)
const errorMessage = ref('')
const notice = ref('')
const members = ref<MemberManagementListItem[]>([])

const filters = reactive({
  keyword: '',
  sortBy: 'userId' as 'userId' | 'memberId' | 'displayName' | 'registerDate',
  sortDirection: 'desc' as 'asc' | 'desc',
})

const sortOptions = [
  { label: '用户编号', value: 'userId' },
  { label: '会员编号', value: 'memberId' },
  { label: '昵称', value: 'displayName' },
  { label: '注册时间', value: 'registerDate' },
]

const statusFilter = ref<'all' | 'active' | 'inactive'>('active')

const visibleMembers = computed(() => {
  if (statusFilter.value === 'active') {
    return members.value.filter(item => item.status !== '0')
  }
  if (statusFilter.value === 'inactive') {
    return members.value.filter(item => item.status === '0')
  }
  return members.value
})


function formatDate(value?: string) {
  if (!value) return '未填写'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '未填写' : date.toLocaleString('zh-CN')
}

function resolveStatusLabel(value?: string) {
  if (value === '1') return '有效'
  if (value === '0') return '已注销'
  return value || '未填写'
}

function resolveBadgeTone(value?: string) {
  return value === '0' ? 'is-inactive' : 'is-active'
}

async function loadMembers() {
  loading.value = true
  errorMessage.value = ''
  notice.value = ''

  try {
    members.value = await getMemberManagementList({
      keyword: filters.keyword.trim() || undefined,
      sortBy: filters.sortBy,
      sortDirection: filters.sortDirection,
    })
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '会员列表加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

function searchMembers() {
  loadMembers()
}

function resetFilters() {
  filters.keyword = ''
  filters.sortBy = 'userId'
  filters.sortDirection = 'desc'
  loadMembers()
}

function toggleSortDirection() {
  filters.sortDirection = filters.sortDirection === 'asc' ? 'desc' : 'asc'
  loadMembers()
}

const detailMemberId = ref<number | null>(null)
const detailMember = computed(() => members.value.find(item => item.memberId === detailMemberId.value) ?? null)

function openDetail(memberId: number) {
  detailMemberId.value = memberId
}

function closeDetail() {
  detailMemberId.value = null
}

async function handleCancel(member: MemberManagementListItem) {
  if (!confirm(`确定要注销会员「${member.displayName}」吗？`)) {
    return
  }

  savingId.value = member.memberId
  notice.value = ''
  errorMessage.value = ''

  try {
    await cancelMember(member.memberId)
    notice.value = '会员已注销。'
    await loadMembers()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '注销失败，请稍后重试。'
  } finally {
    savingId.value = null
  }
}

onMounted(loadMembers)
</script>

<template>
  <div class="members-view">
    <section class="page-head">
      <div>
        <p class="eyebrow">管理员端</p>
        <h1>会员管理</h1>
      </div>
      <div class="head-actions">
        <button type="button" class="btn-ghost" @click="resetFilters">重置</button>
        <button type="button" class="btn-primary" @click="loadMembers">重新加载</button>
      </div>
    </section>

    <section class="filter-bar">
      <div class="search-group">
        <input v-model="filters.keyword" class="search-input" type="text" placeholder="搜索 MemberID / UserID / 姓名 / 手机号" @keyup.enter="searchMembers" />
        <button type="button" class="btn-primary search-btn" @click="searchMembers">搜索</button>
      </div>
      <div class="control-group">
        <select v-model="statusFilter" class="select-input compact-select" @change="loadMembers">
          <option value="all">全部</option>
          <option value="active">有效账户</option>
          <option value="inactive">无效账户</option>
        </select>
        <select v-model="filters.sortBy" class="select-input compact-select" @change="loadMembers">
          <option v-for="item in sortOptions" :key="item.value" :value="item.value">按{{ item.label }}排序</option>
        </select>
        <button type="button" class="btn-ghost" @click="toggleSortDirection">
          {{ filters.sortDirection === 'asc' ? '升序' : '降序' }}
        </button>
      </div>
    </section>

    <div v-if="notice" class="notice-banner">{{ notice }}</div>

    <StateCard v-if="errorMessage" :message="errorMessage" type="error" />

    <div v-else-if="loading" class="loading-state">加载中...</div>

    <section v-else class="table-card">
      <div class="table-head">
        <span>共 {{ visibleMembers.length }} 条</span>
      </div>

      <div v-if="!visibleMembers.length" class="empty-state">暂无会员数据</div>

      <div v-else class="member-list">
        <article v-for="member in visibleMembers" :key="member.memberId" class="member-row">
          <div class="avatar">{{ member.realName?.slice(0, 1) || 'M' }}</div>
          <div class="main-info">
            <strong>{{ member.realName }}</strong>
            <span>#{{ member.memberId }}</span>
          </div>
          <div class="row-actions">
            <span class="status-pill" :class="resolveBadgeTone(member.status)">{{ resolveStatusLabel(member.status) }}</span>
            <button type="button" class="btn-ghost" @click="openDetail(member.memberId)">详情</button>
            <button type="button" class="btn-danger" :disabled="savingId === member.memberId || member.status === '0'" @click="handleCancel(member)">
              {{ savingId === member.memberId ? '注销中...' : '注销' }}
            </button>
          </div>
        </article>
      </div>
    </section>

    <div v-if="detailMember" class="detail-mask" @click.self="closeDetail">
      <section class="detail-popup">
        <div class="detail-popup-head">
          <div>
            <p class="eyebrow">会员详情</p>
            <h2>{{ detailMember.realName }}</h2>
            <p>会员编号 #{{ detailMember.memberId }}</p>
          </div>
          <button type="button" class="btn-ghost" @click="closeDetail">关闭</button>
        </div>

        <div class="detail-grid">
          <div class="detail-item">
            <span class="detail-label">用户编号</span>
            <strong class="detail-value">#{{ detailMember.userId }}</strong>
          </div>
          <div class="detail-item">
            <span class="detail-label">昵称</span>
            <strong class="detail-value">{{ detailMember.displayName }}</strong>
          </div>
          <div class="detail-item">
            <span class="detail-label">手机号</span>
            <strong class="detail-value">{{ detailMember.phoneNumber || '未填写' }}</strong>
          </div>
          <div class="detail-item">
            <span class="detail-label">等级</span>
            <strong class="detail-value">{{ detailMember.memberLevel || '未填写' }}</strong>
          </div>
          <div class="detail-item">
            <span class="detail-label">注册时间</span>
            <strong class="detail-value">{{ formatDate(detailMember.registerDate) }}</strong>
          </div>
          <div class="detail-item">
            <span class="detail-label">状态</span>
            <strong class="detail-value status-text" :class="resolveBadgeTone(detailMember.status)">{{ resolveStatusLabel(detailMember.status) }}</strong>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.members-view {
  display: grid;
  gap: 18px;
}

.page-head,
.filter-bar,
.notice-banner,
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
.main-info span,
.meta-info span {
  color: #6b7280;
}

.eyebrow,
.subtext,
.table-head span {
  margin: 0;
}

.page-head h1 {
  margin: 4px 0 6px;
  font-size: 28px;
}

.filter-bar {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 12px;
  align-items: center;
}

.search-group {
  display: flex;
  gap: 8px;
  width: 100%;
  min-width: 0;
}

.control-group {
  display: flex;
  gap: 6px;
  justify-content: flex-end;
}

.search-input,
.select-input {
  width: 100%;
  border: 1px solid #d1d5db;
  border-radius: 10px;
  padding: 9px 12px;
  outline: none;
}

.search-input {
  min-width: 0;
  flex: 1 1 auto;
}

.compact-select {
  width: 144px;
}

.search-btn {
  flex: 0 0 auto;
}

.member-list {
  display: grid;
  gap: 12px;
  margin-top: 14px;
}

.member-row {
  display: grid;
  grid-template-columns: 44px minmax(0, 1fr) auto;
  gap: 18px;
  align-items: center;
  padding: 14px 16px;
  border: 1px solid #eef2f7;
  border-radius: 12px;
}

.detail-mask {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.28);
  display: grid;
  place-items: center;
  padding: 20px;
}

.detail-popup {
  width: min(760px, 100%);
  background: #fff;
  border-radius: 18px;
  border: 1px solid #e5e7eb;
  padding: 18px;
  box-shadow: 0 20px 50px rgba(15, 23, 42, 0.16);
}

.detail-popup-head {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
  padding-bottom: 14px;
  border-bottom: 1px solid #f1f5f9;
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
  margin-top: 16px;
}

.detail-item {
  display: grid;
  gap: 10px;
  padding: 14px 16px;
  border-radius: 14px;
  background: #f8fbff;
  border: 1px solid #e6edf8;
}

.detail-item.full-width {
  grid-column: 1 / -1;
}

.detail-label {
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: #64748b;
}

.detail-value {
  color: #0f172a;
  font-size: 15px;
  font-weight: 700;
  line-height: 1.5;
}

.detail-value.status-text {
  display: inline-flex;
  align-items: center;
  width: fit-content;
  padding: 6px 10px;
  border-radius: 999px;
  font-size: 13px;
}

.member-row:nth-child(odd) {
  background: #f8fafc;
}

.member-row:nth-child(even) {
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
}

.main-info span {
  color: #6b7280;
}

.row-actions {
  display: flex;
  gap: 8px;
  align-items: center;
}

.status-pill {
  border-radius: 999px;
  padding: 4px 8px;
  font-size: 12px;
  white-space: nowrap;
}

.status-pill.is-active {
  background: #dcfce7;
  color: #166534;
}

.status-pill.is-inactive {
  background: #fee2e2;
  color: #991b1b;
}

.btn-primary,
.btn-ghost,
.btn-danger {
  border: none;
  border-radius: 10px;
  padding: 10px 14px;
  cursor: pointer;
}

.btn-primary {
  background: #2563eb;
  color: #fff;
}

.btn-ghost {
  background: #eff6ff;
  color: #2563eb;
}

.btn-danger {
  background: #fee2e2;
  color: #b91c1c;
}

.loading-state,
.empty-state {
  padding: 28px;
  text-align: center;
  color: #6b7280;
}

@media (max-width: 960px) {
  .page-head,
  .filter-bar,
  .member-row {
    grid-template-columns: 1fr;
  }

  .page-head {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
