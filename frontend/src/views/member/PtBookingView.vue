<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ApiError } from '@/api/http'
import {
  getMemberPersonalPackages,
  type PersonalPackage,
} from '@/api/personal-packages'
import {
  cancelPtBooking,
  createPtBooking,
  getMemberPtBookings,
  type PtBooking,
  type PtBookingStatus,
} from '@/api/pt-bookings'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const packages = ref<PersonalPackage[]>([])
const bookings = ref<PtBooking[]>([])
const loading = ref(true)
const submitting = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const form = reactive({
  packageId: '',
  sessionTime: '',
})

const memberId = computed(() =>
  authStore.session?.userType === 'member' ? authStore.session.userId : 1,
)
const usablePackages = computed(() => packages.value.filter((item) => item.isUsable))

const statusText: Record<PtBookingStatus, string> = {
  PENDING: '待教练确认',
  CONFIRMED: '已确认并消课',
  REJECTED: '教练已拒绝',
  CANCELLED: '已取消',
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('zh-CN', {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

async function loadData() {
  loading.value = true
  errorMessage.value = ''

  try {
    const [packageResult, bookingResult] = await Promise.all([
      getMemberPersonalPackages(memberId.value),
      getMemberPtBookings(memberId.value),
    ])
    packages.value = packageResult
    bookings.value = bookingResult

    if (!form.packageId && usablePackages.value.length > 0) {
      form.packageId = String(usablePackages.value[0]?.packageId)
    }
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '私教预约数据加载失败。'
  } finally {
    loading.value = false
  }
}

async function submitBooking() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!form.packageId || !form.sessionTime) {
    errorMessage.value = '请选择可用课包和上课时间。'
    return
  }

  submitting.value = true
  try {
    await createPtBooking({
      memberId: memberId.value,
      packageId: Number(form.packageId),
      sessionTime: new Date(form.sessionTime).toISOString(),
    })
    successMessage.value = '预约已提交，请等待教练确认。'
    form.sessionTime = ''
    await loadData()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '预约提交失败。'
  } finally {
    submitting.value = false
  }
}

async function cancelBooking(booking: PtBooking) {
  if (!window.confirm(`确定取消 ${formatDateTime(booking.sessionTime)} 的预约吗？`)) {
    return
  }

  errorMessage.value = ''
  successMessage.value = ''
  try {
    await cancelPtBooking(booking.ptBookingId, memberId.value)
    successMessage.value = '预约已取消。'
    await loadData()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '取消预约失败。'
  }
}

onMounted(loadData)
</script>

<template>
  <div>
    <PageHeader
      eyebrow="PT Booking"
      title="私教预约"
      subtitle="使用有效课包选择上课时间；系统会检查课包有效期、剩余次数以及会员和教练的时间冲突。"
    />

    <StateCard v-if="loading" message="私教预约数据加载中..." />

    <template v-else>
      <p v-if="successMessage" class="notice success">{{ successMessage }}</p>
      <p v-if="errorMessage" class="notice error">{{ errorMessage }}</p>

      <section class="booking-layout">
        <article class="panel booking-form">
          <div class="panel-head">
            <div>
              <p>新建预约</p>
              <h2>选择课包与时间</h2>
            </div>
          </div>

          <StateCard
            v-if="usablePackages.length === 0"
            message="没有可用课包，暂时无法预约。请检查剩余次数和有效期。"
          />

          <form v-else @submit.prevent="submitBooking">
            <label>
              <span>私教课包</span>
              <select v-model="form.packageId">
                <option v-for="item in usablePackages" :key="item.packageId" :value="String(item.packageId)">
                  {{ item.courseName }} · {{ item.coachName }}（剩余 {{ item.remainingSessions }} 次）
                </option>
              </select>
            </label>
            <label>
              <span>计划上课时间</span>
              <input v-model="form.sessionTime" type="datetime-local" required />
            </label>
            <button type="submit" :disabled="submitting">
              {{ submitting ? '提交中...' : '提交预约' }}
            </button>
          </form>
        </article>

        <article class="panel">
          <div class="panel-head">
            <div>
              <p>Booking History</p>
              <h2>我的预约</h2>
            </div>
            <button type="button" class="link-btn" @click="loadData">刷新</button>
          </div>

          <StateCard v-if="bookings.length === 0" message="暂时没有私教预约记录。" />

          <div v-else class="booking-list">
            <div v-for="item in bookings" :key="item.ptBookingId" class="booking-item">
              <div>
                <p class="course">{{ item.courseName }}</p>
                <p>{{ item.coachName }}教练 · {{ formatDateTime(item.sessionTime) }}</p>
              </div>
              <div class="item-actions">
                <span class="badge" :class="item.status.toLowerCase()">
                  {{ statusText[item.status] }}
                </span>
                <button
                  v-if="item.status === 'PENDING'"
                  type="button"
                  class="cancel-btn"
                  @click="cancelBooking(item)"
                >
                  取消
                </button>
              </div>
            </div>
          </div>
        </article>
      </section>
    </template>
  </div>
</template>

<style scoped>
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

.booking-layout {
  display: grid;
  grid-template-columns: minmax(280px, 0.8fr) minmax(380px, 1.5fr);
  gap: 20px;
  align-items: start;
}

.panel {
  padding: 22px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.panel-head {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: flex-start;
  margin-bottom: 18px;
}

.panel-head p {
  margin: 0;
  color: #4d77ff;
  font-size: 12px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.panel-head h2 {
  margin: 5px 0 0;
  color: var(--tj-text);
}

form,
label {
  display: grid;
  gap: 8px;
}

form {
  gap: 16px;
}

label span {
  color: var(--tj-text-muted);
  font-size: 13px;
}

select,
input {
  width: 100%;
  box-sizing: border-box;
  border: 1px solid var(--tj-border);
  border-radius: 10px;
  padding: 11px 12px;
  background: white;
  color: var(--tj-text);
}

form button {
  border: 0;
  border-radius: 10px;
  padding: 12px 16px;
  background: #315fe8;
  color: white;
  cursor: pointer;
}

form button:disabled {
  opacity: 0.55;
  cursor: wait;
}

.link-btn,
.cancel-btn {
  border: 0;
  background: transparent;
  cursor: pointer;
}

.link-btn {
  color: #315fe8;
}

.cancel-btn {
  color: var(--tj-danger);
}

.booking-list {
  display: grid;
  gap: 12px;
}

.booking-item {
  display: flex;
  justify-content: space-between;
  gap: 18px;
  align-items: center;
  padding: 15px 0;
  border-top: 1px solid var(--tj-border);
}

.booking-item p {
  margin: 4px 0 0;
  color: var(--tj-text-muted);
}

.booking-item .course {
  margin: 0;
  color: var(--tj-text);
  font-weight: 700;
}

.item-actions {
  display: flex;
  gap: 10px;
  align-items: center;
}

.badge {
  padding: 5px 9px;
  border-radius: 999px;
  background: #fff7e6;
  color: #9a6700;
  font-size: 12px;
  white-space: nowrap;
}

.badge.confirmed {
  background: #e9f8ef;
  color: #187342;
}

.badge.cancelled,
.badge.rejected {
  background: #f5eeee;
  color: #a13a3a;
}

@media (max-width: 900px) {
  .booking-layout {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 560px) {
  .booking-item {
    display: grid;
  }

  .item-actions {
    justify-content: space-between;
  }
}
</style>
