<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ApiError } from '@/api/http'
import { getCoachSchedules, type CoachScheduleItem } from '@/api/coach-schedules'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const coachId = computed(() => authStore.session?.userId ?? 101)

const schedules = ref<CoachScheduleItem[]>([])
const loading = ref(true)
const errorMessage = ref('')

// 与私教预约一致：库内按北京时间墙钟存储，去掉误带的 Z 再按本地展示。
function toLocalDate(value: string) {
  return new Date(value.endsWith('Z') ? value.slice(0, -1) : value)
}

function formatDate(value: string) {
  return toLocalDate(value).toLocaleDateString('zh-CN')
}

function formatTime(value: string) {
  return toLocalDate(value).toLocaleTimeString('zh-CN', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  })
}

function formatType(type: string | null) {
  if (type === 'G') return '团操'
  if (type === 'P') return '私教'
  return type ?? '未知'
}

function formatStatus(status: string | null) {
  return status ?? '未知'
}

function statusClass(status: string | null) {
  if (status === '正在进行中') return 'status-live'
  if (status === '已取消') return 'status-cancelled'
  if (status === '已完成') return 'status-done'
  return ''
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

async function loadSchedules() {
  loading.value = true
  errorMessage.value = ''

  try {
    schedules.value = await getCoachSchedules(coachId.value)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '日程加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

onMounted(loadSchedules)
</script>

<template>
  <div class="coach-schedule">
    <PageHeader
      eyebrow="Coach Schedule"
      title="教练日程"
      subtitle="仅显示未开始或正在进行中的授课安排；已过结束时间的课程不再展示。"
    >
      <template #actions>
        <button type="button" class="refresh-btn" :disabled="loading" @click="loadSchedules">
          刷新
        </button>
      </template>
    </PageHeader>

    <StateCard v-if="loading" message="日程加载中..." />
    <StateCard v-else-if="errorMessage" :message="errorMessage" type="error" />

    <template v-else>
      <p v-if="schedules.length === 0" class="empty-tip">暂无日程安排。</p>

      <section v-else class="schedule-list">
        <article
          v-for="item in schedules"
          :key="item.scheduleId"
          class="schedule-card"
          :class="{ 'is-conflict': item.isConflict }"
        >
          <div class="card-top">
            <div class="title-block">
              <div class="badges">
                <span class="schedule-type" :class="item.scheduleType === 'P' ? 'type-pt' : 'type-group'">
                  {{ formatType(item.scheduleType) }}
                </span>
                <span v-if="item.isConflict" class="conflict-badge">冲突</span>
                <span class="schedule-status" :class="statusClass(item.status)">{{ formatStatus(item.status) }}</span>
              </div>
              <h3>{{ courseTitle(item) }}</h3>
              <p class="time-line">
                {{ formatDate(item.scheduleDate) }} ·
                {{ formatTime(item.scheduleStart) }} - {{ formatTime(item.scheduleEnd) }}
              </p>
            </div>
          </div>

          <dl class="detail-grid">
            <div>
              <dt>预约会员</dt>
              <dd>{{ memberLabel(item) }}</dd>
            </div>
            <div>
              <dt>课程</dt>
              <dd>{{ courseTitle(item) }}</dd>
            </div>
            <div v-if="item.sourceRecordId">
              <dt>预约编号</dt>
              <dd>#{{ item.sourceRecordId }}</dd>
            </div>
          </dl>
        </article>
      </section>
    </template>
  </div>
</template>

<style scoped>
.coach-schedule {
  display: grid;
  gap: 20px;
}

.refresh-btn {
  border: 0;
  border-radius: 10px;
  padding: 10px 18px;
  background: #315fe8;
  color: white;
  cursor: pointer;
  font-weight: 600;
}

.refresh-btn:disabled {
  opacity: 0.55;
  cursor: wait;
}

.empty-tip {
  margin: 0;
  padding: 32px 20px;
  border-radius: 12px;
  background: var(--tj-card-bg, #fff);
  color: #7a88a0;
  text-align: center;
}

.schedule-list {
  display: grid;
  gap: 14px;
}

.schedule-card {
  padding: 22px 24px;
  border-radius: var(--tj-radius, 14px);
  background: var(--tj-card-bg, #fff);
  box-shadow: var(--tj-shadow, 0 2px 10px rgba(20, 34, 57, 0.06));
  border: 1px solid transparent;
}

.schedule-card.is-conflict {
  border-color: #f56c6c;
  background: #fff5f5;
}

.card-top {
  display: flex;
  justify-content: space-between;
  gap: 16px;
}

.title-block h3 {
  margin: 10px 0 6px;
  color: #182337;
  font-size: 20px;
}

.time-line {
  margin: 0;
  color: #7a88a0;
  font-size: 14px;
}

.badges {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}

.schedule-type,
.conflict-badge,
.schedule-status {
  padding: 4px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
}

.type-group {
  background: #e8f0ff;
  color: #2c57d2;
}

.type-pt {
  background: #fff0e6;
  color: #d2691e;
}

.conflict-badge {
  background: #fde2e2;
  color: #c0392b;
}

.schedule-status {
  background: #eef2f7;
  color: #5a6a82;
}

.schedule-status.status-live {
  background: #e8f7ee;
  color: #15803d;
}

.schedule-status.status-cancelled {
  background: #f5eeee;
  color: #a13a3a;
}

.schedule-status.status-done {
  background: #eef2f7;
  color: #7a88a0;
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 12px 24px;
  margin: 18px 0 0;
  padding-top: 16px;
  border-top: 1px solid #e8eef7;
}

.detail-grid div {
  min-width: 0;
}

dt {
  color: #7a88a0;
  font-size: 12px;
}

dd {
  margin: 6px 0 0;
  color: #182337;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

@media (max-width: 700px) {
  .title-block h3 {
    font-size: 18px;
  }
}
</style>
