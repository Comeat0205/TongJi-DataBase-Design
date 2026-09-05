<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import PageHeader from '../../components/ui/PageHeader.vue'
import StateCard from '../../components/ui/StateCard.vue'
import {
  cancelGroupCourse,
  getMyGroupBookings,
  type GroupCourseBooking,
} from '../../api/groupCourseBookings'
import { useAuthStore } from '../../stores/auth'

const authStore = useAuthStore()

const memberId = computed(() => authStore.session?.userId)

const bookings = ref<GroupCourseBooking[]>([])
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

async function loadBookings() {
  if (!memberId.value) {
    error.value = '未获取到当前会员信息，请先登录'
    loading.value = false
    return
  }

  loading.value = true
  error.value = ''

  try {
    bookings.value = await getMyGroupBookings(memberId.value)
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
      subtitle="查看当前会员的团课预约记录，并取消仍处于已预约状态的课程。"
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

    <StateCard
      v-else-if="bookings.length === 0"
      message="当前暂无团课预约记录。"
    />

    <section
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
            <h2>{{ booking.courseName || `课程 #${booking.courseId}` }}</h2>
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
            <strong>{{ formatBookingTime(booking.bookingTime) }}</strong>
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
    </section>
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

  .booking-info {
    grid-template-columns: 1fr;
  }
}
</style>