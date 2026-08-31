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

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('zh-CN')
}

function formatTime(value: string) {
  return new Date(value).toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit', hour12: false })
}

function formatType(type: string | null) {
  if (type === 'G') return '团操'
  if (type === 'P') return '私教'
  return type ?? '未知'
}

function formatStatus(status: string | null) {
  return status ?? '未知'
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
      subtitle="查看你的授课安排（数据来自共享库 COACH_SCHEDULE）。"
    />

    <StateCard v-if="loading" message="日程加载中..." />
    <StateCard v-else-if="errorMessage" :message="errorMessage" type="error" />

    <template v-else>
      <p v-if="schedules.length === 0" class="empty-tip">暂无日程安排。</p>

      <section v-else class="schedule-list">
        <article v-for="item in schedules" :key="item.scheduleId" class="schedule-card">
          <div class="schedule-main">
            <span class="schedule-type" :class="item.scheduleType === 'P' ? 'type-pt' : 'type-group'">
              {{ formatType(item.scheduleType) }}
            </span>
            <h3 class="schedule-time">
              {{ formatTime(item.scheduleStart) }} - {{ formatTime(item.scheduleEnd) }}
            </h3>
            <span class="schedule-status">{{ formatStatus(item.status) }}</span>
          </div>
          <div class="schedule-meta">
            <span>日期：{{ formatDate(item.scheduleDate) }}</span>
            <span v-if="item.sourceRecordId">来源记录：#{{ item.sourceRecordId }}</span>
          </div>
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
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
}

.schedule-main {
  display: flex;
  align-items: center;
  gap: 14px;
}

.schedule-type {
  padding: 6px 12px;
  border-radius: 999px;
  font-size: 13px;
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

.schedule-time {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  color: #182337;
}

.schedule-status {
  padding: 4px 10px;
  border-radius: 999px;
  background: #eef2f7;
  color: #5a6a82;
  font-size: 12px;
}

.schedule-meta {
  display: flex;
  gap: 16px;
  color: #7a88a0;
  font-size: 13px;
}

@media (max-width: 700px) {
  .schedule-card {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
