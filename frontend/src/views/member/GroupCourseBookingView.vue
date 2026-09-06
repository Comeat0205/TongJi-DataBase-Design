<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import PageHeader from '../../components/ui/PageHeader.vue'
import StateCard from '../../components/ui/StateCard.vue'
import {
  cancelGroupCourse,
  getMyGroupBookings,
  type GroupCourseBooking,
} from '../../api/groupCourseBookings'
import {
  getMyWaitingQueues,
  type WaitingQueue,
} from '../../api/waitingQueues'
import {
  getMyAbsenceRecords,
  type AbsenceRecord,
} from '../../api/absenceRecords'
import { useAuthStore } from '../../stores/auth'

const authStore = useAuthStore()

const memberId = computed(() => authStore.session?.userId)

const bookings = ref<GroupCourseBooking[]>([])
const waitingQueues = ref<WaitingQueue[]>([])
const absenceRecords = ref<AbsenceRecord[]>([])

const loading = ref(true)
const error = ref('')

const cancellingCourseId = ref<number | null>(null)
const message = ref('')
const messageType = ref<'success' | 'error'>('success')

function getStatusText(status: string) {
  switch (status) {
    case '0':
      return '待确认'
    case '1':
      return '已预约'
    case '2':
      return '已取消'
    case '3':
      return '已完成'
    default:
      return '未知状态'
  }
}

function isActiveBooking(booking: GroupCourseBooking) {
  return booking.bookingStatus === '1'
}

function formatBookingTime(time: string | null) {
  if (!time) {
    return '暂无'
  }

  const date = new Date(time)

  if (Number.isNaN(date.getTime())) {
    return time
  }

  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function formatWaitingTime(time: string | null) {
  return formatBookingTime(time)
}

function formatCourseDate(date: string) {
  if (!date) {
    return '暂无'
  }

  const value = new Date(date)

  if (Number.isNaN(value.getTime())) {
    return date
  }

  return value.toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  })
}

function formatAbsenceTime(time: string | null) {
  return formatBookingTime(time)
}

async function loadBookings() {
  if (!memberId.value) {
    error.value = '未获取到当前会员信息，请先登录'
    loading.value = false
    return
  }

  loading.value = true
  error.value = ''

  try {
    const [bookingData, waitingData, absenceData] = await Promise.all([
  getMyGroupBookings(memberId.value),
  getMyWaitingQueues(memberId.value),
  getMyAbsenceRecords(memberId.value),
])

bookings.value = bookingData
waitingQueues.value = waitingData
absenceRecords.value = absenceData
  } catch (err) {
    error.value =
      err instanceof Error
        ? err.message
        : '预约记录加载失败，请稍后重试'
  } finally {
    loading.value = false
  }
}

async function handleCancel(booking: GroupCourseBooking) {
  if (!memberId.value) {
    messageType.value = 'error'
    message.value = '未获取到当前会员信息，请先登录'
    return
  }

  if (!isActiveBooking(booking)) {
    messageType.value = 'error'
    message.value = '当前预约状态不允许取消'
    return
  }

  const confirmed = window.confirm(
    `确定要取消「${booking.courseName}」的预约吗？`,
  )

  if (!confirmed) {
    return
  }

  cancellingCourseId.value = booking.courseId
  message.value = ''

  try {
    await cancelGroupCourse(
      memberId.value,
      booking.courseId,
    )

    messageType.value = 'success'
    message.value = '取消预约成功'

    await loadBookings()
  } catch (err) {
    messageType.value = 'error'
    message.value =
      err instanceof Error
        ? err.message
        : '取消预约失败，请稍后重试'
  } finally {
    cancellingCourseId.value = null
  }
}

onMounted(() => {
  loadBookings()
})
</script>

<template>
  <div class="group-booking-page">
    <PageHeader
      eyebrow="MY GROUP BOOKINGS"
      title="我的团课预约"
      subtitle="查看当前会员的正式预约和候补课程，并取消仍处于已预约状态的课程。"
    />

    <div
      v-if="message"
      class="booking-message"
      :class="messageType"
    >
      {{ message }}
    </div>

    <StateCard
      v-if="loading"
      message="正在加载预约记录..."
    />

    <StateCard
      v-else-if="error"
      :message="error"
      type="error"
    />

    <template v-else>
      <!-- 候补中的课程 -->
      <section
        v-if="waitingQueues.length > 0"
        class="booking-section"
      >
        <div class="section-header">
          <div>
            <span class="section-eyebrow">WAITING QUEUE</span>
            <h2>候补中的课程</h2>
          </div>

          <span class="section-count">
            {{ waitingQueues.length }} 门
          </span>
        </div>

        <div class="booking-list">
          <article
            v-for="queue in waitingQueues"
            :key="queue.queueId"
            class="booking-card waiting-card"
          >
            <div class="booking-header">
              <div>
                <span class="booking-type waiting-type">
                  候补课程
                </span>
                <h2>
                  {{ queue.courseName || `课程 #${queue.courseId}` }}
                </h2>
              </div>

              <span class="status waiting">
                候补中
              </span>
            </div>

            <div class="booking-info">
              <div class="info-item">
                <span class="label">候补编号</span>
                <strong>{{ queue.queueId }}</strong>
              </div>

              <div class="info-item">
                <span class="label">课程编号</span>
                <strong>{{ queue.courseId }}</strong>
              </div>

              <div class="info-item">
                <span class="label">加入时间</span>
                <strong>
                  {{ formatWaitingTime(queue.enqueueTime) }}
                </strong>
              </div>
            </div>

            <div class="waiting-hint">
              当前课程暂无空位，请等待名额释放后自动转为正式预约。
            </div>
          </article>
        </div>
      </section>

      <!-- 正式预约 -->
      <section class="booking-section">
        <div class="section-header">
          <div>
            <span class="section-eyebrow">CONFIRMED BOOKINGS</span>
            <h2>正式预约</h2>
          </div>

          <span
            v-if="bookings.length > 0"
            class="section-count"
          >
            {{ bookings.length }} 门
          </span>
        </div>

        <StateCard
          v-if="bookings.length === 0"
          message="当前暂无正式团课预约。"
        />

        <div
          v-else
          class="booking-list"
        >
          <article
            v-for="booking in bookings"
            :key="booking.bookingId"
            class="booking-card"
          >
            <div class="booking-header">
              <div>
                <span class="booking-type">团课预约</span>
                <h2>
                  {{ booking.courseName || `课程 #${booking.courseId}` }}
                </h2>
              </div>

              <span
                class="status"
                :class="{
                  active: booking.bookingStatus === '1',
                  cancelled: booking.bookingStatus === '2',
                  completed: booking.bookingStatus === '3',
                }"
              >
                {{ getStatusText(booking.bookingStatus) }}
              </span>
            </div>

            <div class="booking-info">
              <div class="info-item">
                <span class="label">预约编号</span>
                <strong>{{ booking.bookingId }}</strong>
              </div>

              <div class="info-item">
                <span class="label">课程编号</span>
                <strong>{{ booking.courseId }}</strong>
              </div>

              <div class="info-item">
                <span class="label">预约时间</span>
                <strong>
                  {{ formatBookingTime(booking.bookingTime) }}
                </strong>
              </div>
            </div>

            <div class="booking-actions">
              <button
                v-if="isActiveBooking(booking)"
                type="button"
                class="cancel-button"
                :disabled="cancellingCourseId === booking.courseId"
                @click="handleCancel(booking)"
              >
                {{
                  cancellingCourseId === booking.courseId
                    ? '取消中...'
                    : '取消预约'
                }}
              </button>

              <span
                v-else-if="booking.bookingStatus === '2'"
                class="cancelled-hint"
              >
                该预约已经取消
              </span>

              <span
                v-else
                class="cancelled-hint"
              >
                当前状态不可取消
              </span>
            </div>
          </article>
        </div>
      </section>

      <!-- 缺席记录 -->
      <section class="booking-section">
        <div class="section-header">
          <div>
            <span class="section-eyebrow">ABSENCE RECORDS</span>
            <h2>缺席记录</h2>
          </div>

          <span
            v-if="absenceRecords.length > 0"
            class="section-count"
          >
            {{ absenceRecords.length }} 次
          </span>
        </div>

        <StateCard
          v-if="absenceRecords.length === 0"
          message="当前暂无缺席记录。"
        />

        <div
          v-else
          class="booking-list"
        >
          <article
            v-for="record in absenceRecords"
            :key="record.absenceId"
            class="booking-card absence-card"
          >
            <div class="booking-header">
              <div>
                <span class="booking-type absence-type">
                  缺席记录
                </span>
                <h2>
                  {{ record.courseName || `课程 #${record.bookingId}` }}
                </h2>
              </div>

              <span class="status absence">
                已缺席
              </span>
            </div>

            <div class="booking-info">
              <div class="info-item">
                <span class="label">缺席记录编号</span>
                <strong>{{ record.absenceId }}</strong>
              </div>

              <div class="info-item">
                <span class="label">课程日期</span>
                <strong>
                  {{ formatCourseDate(record.courseDate) }}
                </strong>
              </div>

              <div class="info-item">
                <span class="label">登记时间</span>
                <strong>
                  {{ formatAbsenceTime(record.absenceTime) }}
                </strong>
              </div>
            </div>
          </article>
        </div>
      </section>
    </template>
  </div>
</template>


<style scoped>
.group-booking-page {
  width: 100%;
}

.booking-message {
  margin-bottom: 20px;
  padding: 14px 18px;
  border-radius: 10px;
  font-size: 14px;
  font-weight: 600;
}

.booking-message.success {
  background: #e8f0ff;
  color: var(--tj-primary);
}

.booking-message.error {
  background: #fcebed;
  color: var(--tj-danger);
}

.booking-section {
  margin-bottom: 28px;
}

.section-header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 14px;
}

.section-eyebrow {
  display: block;
  margin-bottom: 5px;
  color: var(--tj-primary);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.08em;
}

.section-header h2 {
  margin: 0;
  color: var(--tj-text);
  font-size: 20px;
}

.section-count {
  flex-shrink: 0;
  padding: 5px 10px;
  border-radius: 999px;
  background: var(--tj-primary-soft);
  color: var(--tj-primary);
  font-size: 12px;
  font-weight: 600;
}

.booking-list {
  display: grid;
  gap: 16px;
}

.booking-card {
  padding: 22px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.waiting-card {
  border-left: 4px solid var(--tj-primary);
}

.absence-card {
  border-left: 4px solid var(--tj-danger);
}

.absence-type {
  color: var(--tj-danger);
}

.status.absence {
  background: #fcebed;
  color: var(--tj-danger);
}

.booking-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.booking-type {
  display: inline-block;
  margin-bottom: 8px;
  color: var(--tj-primary);
  font-size: 12px;
  font-weight: 600;
}

.waiting-type {
  color: var(--tj-primary);
}

.booking-card h2 {
  margin: 0;
  color: var(--tj-text);
  font-size: 21px;
}

.status {
  flex-shrink: 0;
  padding: 6px 10px;
  border-radius: 999px;
  background: #f1f3f5;
  color: var(--tj-text-muted);
  font-size: 12px;
  font-weight: 600;
}

.status.active {
  background: var(--tj-primary-soft);
  color: var(--tj-primary);
}

.status.waiting {
  background: var(--tj-primary-soft);
  color: var(--tj-primary);
}

.status.cancelled {
  background: #fcebed;
  color: var(--tj-danger);
}

.status.completed {
  background: #edf7ed;
  color: #2e7d32;
}

.booking-info {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
  margin-top: 20px;
}

.info-item {
  padding: 12px;
  border-radius: 10px;
  background: var(--tj-page-bg);
}

.info-item .label {
  display: block;
  margin-bottom: 5px;
  color: var(--tj-text-muted);
  font-size: 12px;
}

.info-item strong {
  color: var(--tj-text);
  font-size: 14px;
}

.waiting-hint {
  margin-top: 16px;
  padding: 12px 14px;
  border-radius: 10px;
  background: var(--tj-page-bg);
  color: var(--tj-text-muted);
  font-size: 13px;
  line-height: 1.6;
}

.booking-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 18px;
}

.cancel-button {
  padding: 9px 18px;
  border: none;
  border-radius: 10px;
  background: var(--tj-danger);
  color: white;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}

.cancel-button:hover:not(:disabled) {
  opacity: 0.9;
}

.cancel-button:disabled {
  background: #c8ced9;
  cursor: not-allowed;
}

.cancelled-hint {
  color: var(--tj-text-muted);
  font-size: 13px;
}

@media (max-width: 700px) {
  .booking-header {
    flex-direction: column;
  }

  .section-header {
    align-items: flex-start;
    flex-direction: column;
  }

  .booking-info {
    grid-template-columns: 1fr;
  }
}
</style>
