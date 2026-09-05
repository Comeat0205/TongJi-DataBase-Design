<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ApiError } from '@/api/http'
import {
  confirmPtBooking,
  consumePtBooking,
  getCoachPtBookings,
  type PtBooking,
  type PtBookingStatus,
  undoPtBookingConsumption,
} from '@/api/pt-bookings'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const bookings = ref<PtBooking[]>([])
const loading = ref(true)
const processingId = ref<number | null>(null)
const errorMessage = ref('')
const successMessage = ref('')

const coachId = computed(() =>
  authStore.session?.userType === 'coach' ? authStore.session.userId : 1,
)

const statusText: Record<PtBookingStatus, string> = {
  PENDING: '待确认',
  CONFIRMED: '已确认',
  REJECTED: '已拒绝',
  CANCELLED: '已取消',
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('zh-CN', {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

async function loadBookings() {
  loading.value = true
  errorMessage.value = ''

  try {
    bookings.value = await getCoachPtBookings(coachId.value)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '私教预约加载失败。'
  } finally {
    loading.value = false
  }
}

async function handleBooking(booking: PtBooking, accept: boolean) {
  processingId.value = booking.ptBookingId
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await confirmPtBooking(booking.ptBookingId, coachId.value, accept)
    successMessage.value = accept
      ? `已确认 ${booking.courseName} 预约。`
      : `已拒绝 ${booking.courseName} 预约。`
    await loadBookings()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '预约处理失败。'
  } finally {
    processingId.value = null
  }
}

async function consumeBooking(booking: PtBooking) {
  if (!window.confirm(`确定为 ${formatDateTime(booking.sessionTime)} 的课程消课吗？`)) {
    return
  }

  processingId.value = booking.ptBookingId
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await consumePtBooking(booking.ptBookingId, coachId.value)
    successMessage.value = `已完成 ${booking.courseName} 消课，并扣减课包次数。`
    await loadBookings()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '消课失败。'
  } finally {
    processingId.value = null
  }
}

async function undoConsumption(booking: PtBooking) {
  if (!window.confirm(`确定撤销 ${formatDateTime(booking.sessionTime)} 的消课吗？`)) {
    return
  }

  processingId.value = booking.ptBookingId
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await undoPtBookingConsumption(booking.ptBookingId, coachId.value)
    successMessage.value = `已撤销 ${booking.courseName} 消课，并恢复课包次数。`
    await loadBookings()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '撤销消课失败。'
  } finally {
    processingId.value = null
  }
}

onMounted(loadBookings)
</script>

<template>
  <div>
    <PageHeader
      eyebrow="Coach Confirmation"
      title="私教确认与消课"
      subtitle="先确认或拒绝会员预约；上完课后再消课扣次，误操作可撤销消课。"
    >
      <template #actions>
        <button type="button" class="refresh-btn" :disabled="loading" @click="loadBookings">
          刷新
        </button>
      </template>
    </PageHeader>

    <p v-if="successMessage" class="notice success">{{ successMessage }}</p>
    <p v-if="errorMessage" class="notice error">{{ errorMessage }}</p>

    <StateCard v-if="loading" message="私教预约加载中..." />
    <StateCard v-else-if="bookings.length === 0" message="当前没有私教预约记录。" />

    <section v-else class="confirmation-list">
      <article v-for="item in bookings" :key="item.ptBookingId" class="confirmation-card">
        <div class="booking-main">
          <p class="booking-id">预约 #{{ item.ptBookingId }}</p>
          <h2>{{ item.courseName }}</h2>
          <p class="time">{{ formatDateTime(item.sessionTime) }}</p>
          <span class="status-badge" :class="item.status.toLowerCase()">
            {{ statusText[item.status] }}
          </span>
        </div>

        <div class="meta">
          <div class="meta-row primary">
            <div>
              <span class="label">会员编号</span>
              <strong>#{{ item.memberId }}</strong>
            </div>
            <div>
              <span class="label">使用课包</span>
              <strong>#{{ item.packageId }}</strong>
            </div>
            <div>
              <span class="label">提交时间</span>
              <strong>{{ formatDateTime(item.bookingTime) }}</strong>
            </div>
          </div>
          <div class="meta-row secondary">
            <div>
              <span class="label">消课状态</span>
              <strong>{{ item.isConsumed ? '已消课' : '未消课' }}</strong>
            </div>
            <div v-if="item.consumedTime">
              <span class="label">消课时间</span>
              <strong>{{ formatDateTime(item.consumedTime) }}</strong>
            </div>
          </div>
        </div>

        <div class="actions">
          <template v-if="item.status === 'PENDING'">
            <button
              type="button"
              class="reject-btn"
              :disabled="processingId === item.ptBookingId"
              @click="handleBooking(item, false)"
            >
              拒绝预约
            </button>
            <button
              type="button"
              class="confirm-btn"
              :disabled="processingId === item.ptBookingId"
              @click="handleBooking(item, true)"
            >
              {{ processingId === item.ptBookingId ? '处理中...' : '确认预约' }}
            </button>
          </template>
          <template v-else-if="item.status === 'CONFIRMED'">
            <button
              v-if="item.canUndoConsumption"
              type="button"
              class="undo-btn"
              :disabled="processingId === item.ptBookingId"
              @click="undoConsumption(item)"
            >
              {{ processingId === item.ptBookingId ? '处理中...' : '撤销消课' }}
            </button>
            <button
              v-else-if="item.canConsume"
              type="button"
              class="consume-btn"
              :disabled="processingId === item.ptBookingId"
              @click="consumeBooking(item)"
            >
              {{ processingId === item.ptBookingId ? '处理中...' : '消课' }}
            </button>
            <span v-else class="readonly-status">未到上课时间</span>
          </template>
          <span v-else class="readonly-status">无需处理</span>
        </div>
      </article>
    </section>
  </div>
</template>

<style scoped>
.refresh-btn,
.confirm-btn,
.reject-btn,
.consume-btn,
.undo-btn {
  border: 0;
  border-radius: 10px;
  padding: 10px 16px;
  cursor: pointer;
}

.refresh-btn,
.confirm-btn,
.consume-btn {
  background: #315fe8;
  color: white;
}

.reject-btn {
  background: #fff0f0;
  color: #a13a3a;
}

.undo-btn {
  background: #fff7e6;
  color: #9a6700;
}

button:disabled {
  opacity: 0.55;
  cursor: wait;
}

.notice {
  margin: 0 0 18px;
  padding: 12px 16px;
  border-radius: 10px;
}

.notice.success {
  background: #e9f8ef;
  color: #187342;
}

.notice.error {
  background: #fff0f0;
  color: var(--tj-danger);
}

.confirmation-list {
  display: grid;
  gap: 16px;
}

.confirmation-card {
  display: grid;
  /* 左右固定宽，中间字段跨卡片竖向对齐 */
  grid-template-columns: 220px minmax(0, 1fr) 200px;
  gap: 24px;
  align-items: center;
  padding: 22px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.booking-main {
  min-width: 0;
}

.booking-id {
  margin: 0;
  color: #4d77ff;
  font-size: 12px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.booking-main h2 {
  margin: 6px 0;
  color: var(--tj-text);
  font-size: 18px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.time {
  margin: 0;
  color: var(--tj-text-muted);
}

.status-badge {
  display: inline-flex;
  width: fit-content;
  margin-top: 10px;
  padding: 5px 10px;
  border-radius: 999px;
  background: #fff7e6;
  color: #9a6700;
  font-size: 12px;
}

.status-badge.confirmed {
  background: #e9f8ef;
  color: #187342;
}

.status-badge.cancelled,
.status-badge.rejected {
  background: #f5eeee;
  color: #a13a3a;
}

.meta {
  display: grid;
  gap: 14px;
  min-width: 0;
}

.meta-row {
  display: grid;
  gap: 12px 20px;
}

.meta-row.primary {
  grid-template-columns: 88px 88px minmax(0, 1fr);
}

.meta-row.secondary {
  grid-template-columns: 88px minmax(0, 1fr);
}

.meta .label {
  display: block;
  color: var(--tj-text-muted);
  font-size: 12px;
}

.meta strong {
  display: block;
  margin-top: 5px;
  color: var(--tj-text);
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  justify-content: flex-end;
  align-items: center;
  min-height: 40px;
}

.readonly-status {
  color: var(--tj-text-muted);
  font-size: 13px;
  text-align: right;
}

@media (max-width: 1050px) {
  .confirmation-card {
    grid-template-columns: 1fr;
  }

  .actions {
    justify-content: flex-end;
  }
}

@media (max-width: 560px) {
  .meta-row.primary,
  .meta-row.secondary {
    grid-template-columns: 1fr;
  }

  .actions {
    display: grid;
    grid-template-columns: 1fr 1fr;
  }
}
</style>
