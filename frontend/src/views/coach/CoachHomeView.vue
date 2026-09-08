<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ApiError } from '@/api/http'
import { getCoachSchedules, type CoachScheduleItem } from '@/api/coach-schedules'
import { getPendingCoachPtBookings, type PtBooking } from '@/api/pt-bookings'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'
import { useAuthStore } from '@/stores/auth'

type SessionStatus = 'completed' | 'in-progress' | 'upcoming'

const route = useRoute()
const authStore = useAuthStore()

const basePath = computed(() => (route.path.startsWith('/preview/coach') ? '/preview/coach' : '/coach'))
const displayName = computed(() => authStore.session?.displayName ?? '教练')
const coachId = computed(() =>
  authStore.session?.userType === 'coach' ? authStore.session.userId : 101,
)

const schedules = ref<CoachScheduleItem[]>([])
const pendingPt = ref<PtBooking[]>([])
const loading = ref(true)
const errorMessage = ref('')

function toLocalDate(value: string) {
  return new Date(value.endsWith('Z') ? value.slice(0, -1) : value)
}

function isSameLocalDay(a: Date, b: Date) {
  return (
    a.getFullYear() === b.getFullYear()
    && a.getMonth() === b.getMonth()
    && a.getDate() === b.getDate()
  )
}

function formatTime(value: string) {
  return toLocalDate(value).toLocaleTimeString('zh-CN', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  })
}

function formatDateTime(value: string) {
  return toLocalDate(value).toLocaleString('zh-CN', {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

function resolveSessionStatus(item: CoachScheduleItem, now = new Date()): SessionStatus {
  if (item.status === '正在进行中') {
    return 'in-progress'
  }

  const start = toLocalDate(item.scheduleStart)
  const end = toLocalDate(item.scheduleEnd)
  if (now >= start && now < end) {
    return 'in-progress'
  }
  if (now >= end) {
    return 'completed'
  }
  return 'upcoming'
}

function sessionStatusLabel(status: SessionStatus) {
  switch (status) {
    case 'completed':
      return '已结束'
    case 'in-progress':
      return '进行中'
    case 'upcoming':
      return '待开始'
  }
}

function sessionStatusClass(status: SessionStatus) {
  return `status-${status}`
}

function courseTitle(item: CoachScheduleItem) {
  return item.courseName?.trim() || (item.scheduleType === 'P' ? '私教课' : '团操课')
}

function memberLabel(item: CoachScheduleItem) {
  if (item.memberName?.trim()) {
    return item.memberId ? `${item.memberName}（#${item.memberId}）` : item.memberName
  }
  if (item.memberId) {
    return `会员 #${item.memberId}`
  }
  return item.scheduleType === 'P' ? '会员信息待同步' : '—'
}

const todaySchedules = computed(() => {
  const now = new Date()
  return schedules.value
    .filter((item) => isSameLocalDay(toLocalDate(item.scheduleStart), now))
    .slice()
    .sort(
      (a, b) => toLocalDate(a.scheduleStart).getTime() - toLocalDate(b.scheduleStart).getTime(),
    )
})

const todayGroupCount = computed(
  () => todaySchedules.value.filter((item) => item.scheduleType === 'G').length,
)
const todayPtCount = computed(
  () => todaySchedules.value.filter((item) => item.scheduleType === 'P').length,
)
const upcomingReminders = computed(() => {
  const now = new Date()
  const inTwoHours = now.getTime() + 2 * 60 * 60 * 1000
  return todaySchedules.value.filter((item) => {
    const start = toLocalDate(item.scheduleStart).getTime()
    return start >= now.getTime() && start <= inTwoHours
  }).length
})

const conflictSchedules = computed(() => schedules.value.filter((item) => item.isConflict))
const conflictMessage = computed(() => {
  if (conflictSchedules.value.length === 0) {
    return null
  }

  const titles = conflictSchedules.value
    .slice(0, 3)
    .map((item) => `${courseTitle(item)} ${formatTime(item.scheduleStart)}`)
    .join('、')

  return {
    count: conflictSchedules.value.length,
    relatedSessions: titles,
  }
})

async function loadDashboard() {
  loading.value = true
  errorMessage.value = ''

  try {
    const [scheduleResult, pendingResult] = await Promise.all([
      getCoachSchedules(coachId.value),
      getPendingCoachPtBookings(coachId.value),
    ])
    schedules.value = scheduleResult
    pendingPt.value = pendingResult
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '工作台数据加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

onMounted(loadDashboard)
</script>

<template>
  <div class="coach-home">
    <PageHeader
      eyebrow="Coach Dashboard"
      :title="`${displayName}，教练工作台`"
      subtitle="汇总今日授课、待确认私教与排课冲突，数据与「我的日程」「私教确认」实时同步。"
    >
      <template #actions>
        <div class="header-actions">
          <RouterLink class="primary-link" :to="`${basePath}/pt-confirm`">私教确认</RouterLink>
          <button type="button" class="refresh-btn" :disabled="loading" @click="loadDashboard">
            刷新
          </button>
        </div>
      </template>
    </PageHeader>

    <StateCard v-if="loading" message="工作台加载中..." />
    <StateCard v-else-if="errorMessage" :message="errorMessage" type="error" />

    <template v-else>
      <section class="summary-grid">
        <article class="summary-card">
          <span>今日团课</span>
          <strong>{{ todayGroupCount }}</strong>
          <small>来自教练日程</small>
        </article>
        <article class="summary-card">
          <span>今日私教</span>
          <strong>{{ todayPtCount }}</strong>
          <small>来自教练日程</small>
        </article>
        <article class="summary-card highlight">
          <span>待确认私教</span>
          <strong>{{ pendingPt.length }}</strong>
          <small>与私教确认同步</small>
        </article>
        <article class="summary-card">
          <span>上课提醒</span>
          <strong>{{ upcomingReminders }}</strong>
          <small>未来 2 小时内开课</small>
        </article>
      </section>

      <section v-if="conflictMessage" class="conflict-banner">
        <div>
          <p class="conflict-eyebrow">排课冲突</p>
          <strong>检测到 {{ conflictMessage.count }} 条日程存在时间重叠</strong>
          <p class="conflict-meta">涉及：{{ conflictMessage.relatedSessions }}</p>
        </div>
        <RouterLink class="text-link" :to="`${basePath}/schedule`">查看日程 →</RouterLink>
      </section>

      <section class="dashboard-grid">
        <article class="dashboard-card">
          <div class="card-head">
            <div>
              <p class="card-eyebrow">今日授课</p>
              <h2>课程与私教安排</h2>
            </div>
            <RouterLink class="text-link" :to="`${basePath}/schedule`">完整日程 →</RouterLink>
          </div>

          <p v-if="todaySchedules.length === 0" class="empty-tip">今天暂无未结束的授课安排。</p>

          <div v-else class="session-list">
            <article v-for="session in todaySchedules" :key="session.scheduleId" class="session-item">
              <div class="session-time">
                <strong>{{ formatTime(session.scheduleStart) }}</strong>
                <span>{{ formatTime(session.scheduleEnd) }}</span>
              </div>
              <div class="session-body">
                <div class="session-top">
                  <h3>
                    {{ courseTitle(session) }}
                    <small>{{ session.scheduleType === 'G' ? '团课' : '私教' }}</small>
                  </h3>
                  <span
                    class="status-pill"
                    :class="sessionStatusClass(resolveSessionStatus(session))"
                  >
                    {{ sessionStatusLabel(resolveSessionStatus(session)) }}
                  </span>
                </div>
                <p v-if="session.scheduleType === 'P'" class="meta">会员：{{ memberLabel(session) }}</p>
                <p v-else class="meta">状态：{{ session.status || '正常' }}</p>
              </div>
            </article>
          </div>
        </article>

        <article class="dashboard-card">
          <div class="card-head">
            <div>
              <p class="card-eyebrow">待确认</p>
              <h2>私教预约确认</h2>
            </div>
          </div>

          <p v-if="pendingPt.length === 0" class="empty-tip">当前没有待确认的私教预约。</p>

          <div v-else class="pt-list">
            <article v-for="item in pendingPt" :key="item.ptBookingId" class="pt-item">
              <h3>{{ item.courseName }}</h3>
              <p class="meta">会员 #{{ item.memberId }} · 课包 #{{ item.packageId }}</p>
              <p class="meta">{{ formatDateTime(item.sessionTime) }}</p>
            </article>
          </div>

          <RouterLink class="primary-link block-link" :to="`${basePath}/pt-confirm`">前往私教确认</RouterLink>
        </article>
      </section>
    </template>
  </div>
</template>

<style scoped>
.coach-home {
  display: grid;
  gap: 20px;
}

.header-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
}

.refresh-btn {
  border: 0;
  border-radius: 10px;
  padding: 8px 14px;
  background: #eef3ff;
  color: #285cff;
  font-weight: 600;
  cursor: pointer;
}

.refresh-btn:disabled {
  opacity: 0.55;
  cursor: wait;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.summary-card {
  padding: 16px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.summary-card.highlight {
  border: 1px solid #ffd591;
  background: #fffaf0;
}

.summary-card span,
.summary-card small {
  display: block;
  color: var(--tj-text-muted);
  font-size: 13px;
}

.summary-card strong {
  display: block;
  margin: 8px 0;
  font-size: 28px;
  color: var(--tj-text);
}

.conflict-banner {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: center;
  padding: 16px 18px;
  border-radius: var(--tj-radius);
  background: #fff1f0;
  border: 1px solid #ffccc7;
}

.conflict-eyebrow {
  margin: 0 0 4px;
  color: #cf1322;
  font-size: 12px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.conflict-meta {
  margin: 6px 0 0;
  color: var(--tj-text-muted);
  font-size: 13px;
}

.dashboard-grid {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 16px;
}

.dashboard-card {
  padding: 22px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.card-head {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
  margin-bottom: 16px;
}

.card-eyebrow {
  margin: 0 0 6px;
  color: #4d77ff;
  font-size: 12px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.dashboard-card h2 {
  margin: 0;
  font-size: 22px;
  color: var(--tj-text);
}

.empty-tip {
  margin: 0;
  color: var(--tj-text-muted);
  font-size: 14px;
}

.session-list,
.pt-list {
  display: grid;
  gap: 12px;
}

.session-item {
  display: grid;
  grid-template-columns: 72px 1fr;
  gap: 14px;
  align-items: start;
  padding: 14px;
  border-radius: 14px;
  background: #f8fbff;
  border: 1px solid #e6edf8;
}

.session-time {
  display: grid;
  gap: 2px;
  text-align: center;
}

.session-time strong {
  font-size: 18px;
  color: var(--tj-text);
}

.session-time span {
  font-size: 12px;
  color: var(--tj-text-muted);
}

.session-top {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: center;
}

.session-item h3,
.pt-item h3 {
  margin: 0;
  font-size: 17px;
}

.session-item h3 small {
  margin-left: 8px;
  color: var(--tj-text-muted);
  font-size: 12px;
  font-weight: 500;
}

.meta {
  margin: 6px 0 0;
  color: var(--tj-text-muted);
  font-size: 13px;
}

.status-pill {
  padding: 4px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
  white-space: nowrap;
}

.status-completed {
  background: #eef2f7;
  color: #5f6b7a;
}

.status-in-progress {
  background: #e8f7ef;
  color: #137333;
}

.status-upcoming {
  background: #eef3ff;
  color: #285cff;
}

.pt-item {
  padding: 14px;
  border-radius: 14px;
  background: #f8fbff;
  border: 1px solid #e6edf8;
}

.text-link,
.primary-link {
  color: #285cff;
  font-weight: 600;
  text-decoration: none;
}

.primary-link {
  display: inline-flex;
  padding: 8px 12px;
  border-radius: 10px;
  background: #285cff;
  color: #fff;
}

.block-link {
  display: block;
  width: fit-content;
  margin-top: 14px;
}

@media (max-width: 960px) {
  .summary-grid,
  .dashboard-grid {
    grid-template-columns: 1fr;
  }

  .session-item {
    grid-template-columns: 1fr;
  }

  .conflict-banner {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
