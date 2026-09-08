<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ApiError } from '@/api/http'
import { getMemberSchedules, type MemberScheduleItem } from '@/api/member-schedules'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const memberId = computed(() => authStore.session?.userId ?? 1)

const schedules = ref<MemberScheduleItem[]>([])
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

function formatType(type: string) {
  if (type === 'G') return '团操'
  if (type === 'P') return '私教'
  return type
}

function formatStatus(status: string | null) {
  if (status === '待上课' || status === '0') return '待上课'
  if (status === '上课中') return '上课中'
  if (status === '1') return '已上课'
  if (status === '2' || status === '已取消') return '已取消'
  return status ?? '未知'
}

function statusClass(status: string | null) {
  if (status === '待上课' || status === '0') return 'status-pending'
  if (status === '上课中') return 'status-live'
  if (status === '1') return 'status-done'
  if (status === '2' || status === '已取消') return 'status-cancelled'
  return ''
}

function courseTitle(item: MemberScheduleItem) {
  return item.courseName?.trim() || (item.scheduleType === 'P' ? '私教课' : '团操课')
}

function coachLabel(item: MemberScheduleItem) {
  if (item.coachName?.trim()) {
    return item.coachId ? `${item.coachName}（#${item.coachId}）` : item.coachName
  }
  if (item.coachId) {
    return `教练 #${item.coachId}`
  }
  return '教练信息待同步'
}

async function loadSchedules() {
  loading.value = true
  errorMessage.value = ''

  try {
    schedules.value = await getMemberSchedules(memberId.value)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '日程加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

onMounted(loadSchedules)
</script>

<template>
  <div class="member-schedule">
    <PageHeader
      eyebrow="Member Schedule"
      title="我的日程"
      subtitle="仅显示未开始或进行中的课程：时段前为待上课，时段内为上课中，结束后自动从列表消失。"
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
        <article v-for="item in schedules" :key="item.scheduleId" class="schedule-card">
          <div class="card-top">
            <div class="title-block">
              <div class="badges">
                <span class="schedule-type" :class="item.scheduleType === 'P' ? 'type-pt' : 'type-group'">
                  {{ formatType(item.scheduleType) }}
                </span>
                <span v-if="item.isUpcoming" class="upcoming-badge">即将开课</span>
                <span class="schedule-status" :class="statusClass(item.status)">
                  {{ formatStatus(item.status) }}
                </span>
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
              <dt>课程名称</dt>
              <dd>{{ courseTitle(item) }}</dd>
            </div>
            <div>
              <dt>授课教练</dt>
              <dd>{{ coachLabel(item) }}</dd>
            </div>
            <div>
              <dt>课程类型</dt>
              <dd>{{ formatType(item.scheduleType) }}</dd>
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
.member-schedule {
  display: grid;
  gap: 20px;
}

.refresh-btn {
  padding: 8px 14px;
  border: 1px solid #d8e2f0;
  border-radius: 10px;
  background: #fff;
  color: #2c57d2;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
}

.refresh-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
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
  padding: 20px 24px;
  border-radius: var(--tj-radius, 14px);
  background: var(--tj-card-bg, #fff);
  box-shadow: var(--tj-shadow, 0 2px 10px rgba(20, 34, 57, 0.06));
  display: grid;
  gap: 16px;
}

.card-top {
  display: flex;
  justify-content: space-between;
  gap: 12px;
}

.title-block {
  display: grid;
  gap: 8px;
}

.badges {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}

.schedule-type,
.upcoming-badge,
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

.upcoming-badge {
  background: #fff3cd;
  color: #b3541e;
}

.schedule-status {
  background: #eef2f7;
  color: #5a6a82;
}

.schedule-status.status-pending {
  background: #e8f0ff;
  color: #2c57d2;
}

.schedule-status.status-live {
  background: #fff3cd;
  color: #b3541e;
}

.schedule-status.status-done {
  background: #e6f7ef;
  color: #0a8a4a;
}

.schedule-status.status-cancelled {
  background: #fff1f0;
  color: #cf1322;
}

.title-block h3 {
  margin: 0;
  font-size: 20px;
  font-weight: 700;
  color: #182337;
}

.time-line {
  margin: 0;
  color: #5a6a82;
  font-size: 14px;
}

.detail-grid {
  margin: 0;
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px 20px;
}

.detail-grid div {
  display: grid;
  gap: 4px;
}

.detail-grid dt {
  color: #7a88a0;
  font-size: 12px;
}

.detail-grid dd {
  margin: 0;
  color: #182337;
  font-size: 14px;
  font-weight: 600;
}

@media (max-width: 700px) {
  .detail-grid {
    grid-template-columns: 1fr;
  }
}
</style>
