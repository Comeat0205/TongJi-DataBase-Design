<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ApiError } from '@/api/http'
import {
  confirmPtBooking,
  getPendingCoachPtBookings,
  type PtBooking,
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
    bookings.value = await getPendingCoachPtBookings(coachId.value)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '待确认预约加载失败。'
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
      ? `已确认 ${booking.courseName} 预约并完成消课。`
      : `已拒绝 ${booking.courseName} 预约。`
    await loadBookings()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '预约处理失败。'
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
      subtitle="查看会员提交的私教预约。确认后将扣减对应课包一次剩余次数；拒绝不会扣次。"
    >
      <template #actions>
        <button type="button" class="refresh-btn" :disabled="loading" @click="loadBookings">
          刷新
        </button>
      </template>
    </PageHeader>

    <p v-if="successMessage" class="notice success">{{ successMessage }}</p>
    <p v-if="errorMessage" class="notice error">{{ errorMessage }}</p>

    <StateCard v-if="loading" message="待确认私教预约加载中..." />
    <StateCard v-else-if="bookings.length === 0" message="当前没有待确认的私教预约。" />

    <section v-else class="confirmation-list">
      <article v-for="item in bookings" :key="item.ptBookingId" class="confirmation-card">
        <div class="booking-main">
          <p class="booking-id">预约 #{{ item.ptBookingId }}</p>
          <h2>{{ item.courseName }}</h2>
          <p class="time">{{ formatDateTime(item.sessionTime) }}</p>
        </div>

        <dl>
          <div>
            <dt>会员编号</dt>
            <dd>#{{ item.memberId }}</dd>
          </div>
          <div>
            <dt>使用课包</dt>
            <dd>#{{ item.packageId }}</dd>
          </div>
          <div>
            <dt>提交时间</dt>
            <dd>{{ formatDateTime(item.bookingTime) }}</dd>
          </div>
        </dl>

        <div class="actions">
          <button
            type="button"
            class="reject-btn"
            :disabled="processingId === item.ptBookingId"
            @click="handleBooking(item, false)"
          >
            拒绝
          </button>
          <button
            type="button"
            class="confirm-btn"
            :disabled="processingId === item.ptBookingId"
            @click="handleBooking(item, true)"
          >
            {{ processingId === item.ptBookingId ? '处理中...' : '确认并消课' }}
          </button>
        </div>
      </article>
    </section>
  </div>
</template>

<style scoped>
.refresh-btn,
.confirm-btn,
.reject-btn {
  border: 0;
  border-radius: 10px;
  padding: 10px 16px;
  cursor: pointer;
}

.refresh-btn,
.confirm-btn {
  background: #315fe8;
  color: white;
}

.reject-btn {
  background: #fff0f0;
  color: #a13a3a;
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
  grid-template-columns: minmax(220px, 1fr) minmax(330px, 1.4fr) auto;
  gap: 24px;
  align-items: center;
  padding: 22px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
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
}

.time {
  margin: 0;
  color: var(--tj-text-muted);
}

dl {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 14px;
  margin: 0;
}

dt {
  color: var(--tj-text-muted);
  font-size: 12px;
}

dd {
  margin: 5px 0 0;
  color: var(--tj-text);
  font-weight: 600;
}

.actions {
  display: flex;
  gap: 10px;
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
  dl {
    grid-template-columns: 1fr;
  }

  .actions {
    display: grid;
    grid-template-columns: 1fr 1fr;
  }
}
</style>
