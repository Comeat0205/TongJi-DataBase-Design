<script setup lang="ts">
// 我的团课：课包资产 + 选包预约（按日期选周课场次）+ 预约记录

import { computed, nextTick, onMounted, ref, watch } from 'vue'
import PageHeader from '../../components/ui/PageHeader.vue'
import StateCard from '../../components/ui/StateCard.vue'
import { getGroupCourses, type GroupCourse } from '../../api/groupCourses'
import {
  bookGroupCourse,
  cancelGroupCourseById,
  getMyGroupBookings,
  type GroupCourseBooking,
} from '../../api/groupCourseBookings'
import { getMyGroupPackages, type GroupPackage } from '../../api/group-packages'
import { getMyWaitingQueues, type WaitingQueue } from '../../api/waitingQueues'
import { getMyAbsenceRecords, type AbsenceRecord } from '../../api/absenceRecords'
import { ApiError } from '../../api/http'
import { useAuthStore } from '../../stores/auth'

const authStore = useAuthStore()
const memberId = computed(() => authStore.session?.userId)

const packages = ref<GroupPackage[]>([])
const courses = ref<GroupCourse[]>([])
const bookings = ref<GroupCourseBooking[]>([])
const waitingQueues = ref<WaitingQueue[]>([])
const absenceRecords = ref<AbsenceRecord[]>([])

const loading = ref(true)
const error = ref('')
const message = ref('')
const messageType = ref<'success' | 'error'>('success')

const selectedPackageId = ref<number | null>(null)
const selectedCourseId = ref<number | null>(null)
const selectedCourseDate = ref<string | null>(null)
const bookingBusy = ref(false)
const cancellingId = ref<number | null>(null)

const usablePackages = computed(() => packages.value.filter((p) => p.isUsable))
const selectedPackage = computed(() =>
  usablePackages.value.find((p) => p.packageId === selectedPackageId.value) ?? null,
)

const bookableCourses = computed(() => {
  if (!selectedPackage.value) return []
  return courses.value.filter((c) => c.typeId === selectedPackage.value!.typeId)
})

type DateOption = {
  value: string
  label: string
  startTime: string
}

function toDateKey(value: string | Date) {
  const d = typeof value === 'string' ? new Date(value) : value
  if (Number.isNaN(d.getTime())) return ''
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function weekdayLabel(dateKey: string) {
  const labels = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']
  const d = new Date(`${dateKey}T00:00:00`)
  return labels[d.getDay()] ?? ''
}

function formatHm(value: string) {
  const match = String(value).match(/(\d{2}):(\d{2})/)
  return match ? `${match[1]}:${match[2]}` : value
}

/** 所选团课：当前起一个月内、匹配该课星期几且未开课的日期 */
const availableDates = computed((): DateOption[] => {
  const course = bookableCourses.value.find((c) => c.courseId === selectedCourseId.value)
  if (!course) return []

  const now = new Date()
  const end = new Date(now)
  end.setDate(end.getDate() + 30)

  const bookedDateKeys = new Set(
    bookings.value
      .filter(
        (b) =>
          b.courseId === course.courseId &&
          b.bookingStatus === '1' &&
          (b.courseDate || b.courseStartTime),
      )
      .map((b) => toDateKey(b.courseDate || b.courseStartTime || '')),
  )

  const options: DateOption[] = []
  const slots = [...(course.timeSlots ?? [])].sort(
    (a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime(),
  )

  for (const slot of slots) {
    const start = new Date(slot.startTime)
    if (Number.isNaN(start.getTime())) continue
    if (start <= now || start > end) continue
    if (course.currentCapacity >= course.maxCapacity) continue

    const dateKey = toDateKey(slot.courseDate)
    if (!dateKey || options.some((o) => o.value === dateKey)) continue
    if (bookedDateKeys.has(dateKey)) continue

    options.push({
      value: dateKey,
      label: `${weekdayLabel(dateKey)} ${dateKey} ${formatHm(slot.startTime)}-${formatHm(slot.endTime)}`,
      startTime: slot.startTime,
    })
  }

  // 若暂无实例，按周模式推演一个月内的对应星期
  if (options.length === 0 && slots[0]) {
    const sample = new Date(slots[0].courseDate)
    const targetDow = sample.getDay()
    const startHm = formatHm(slots[0].startTime)
    const endHm = formatHm(slots[0].endTime)
    const cursor = new Date(now)
    cursor.setHours(0, 0, 0, 0)
    while (cursor <= end) {
      if (cursor.getDay() === targetDow && cursor >= new Date(now.toDateString())) {
        const [hh, mm] = startHm.split(':').map(Number)
        const startAt = new Date(cursor)
        startAt.setHours(hh ?? 0, mm ?? 0, 0, 0)
        if (startAt > now && course.currentCapacity < course.maxCapacity) {
          const dateKey = toDateKey(cursor)
          if (!bookedDateKeys.has(dateKey)) {
            options.push({
              value: dateKey,
              label: `${weekdayLabel(dateKey)} ${dateKey} ${startHm}-${endHm}`,
              startTime: startAt.toISOString(),
            })
          }
        }
      }
      cursor.setDate(cursor.getDate() + 1)
    }
  }

  return options
})

watch(selectedPackageId, async () => {
  selectedCourseId.value = null
  selectedCourseDate.value = null
  await nextTick()
  if (bookableCourses.value.length === 1) {
    selectedCourseId.value = bookableCourses.value[0]!.courseId
  }
})

watch(selectedCourseId, () => {
  selectedCourseDate.value = null
})

watch(availableDates, (dates) => {
  if (dates.length === 1) {
    selectedCourseDate.value = dates[0]!.value
  }
})

async function loadAll() {
  if (!memberId.value) {
    error.value = '请先登录'
    loading.value = false
    return
  }
  loading.value = true
  error.value = ''
  try {
    const [pkg, courseData, bookingData, waitingData, absenceData] = await Promise.all([
      getMyGroupPackages(memberId.value),
      getGroupCourses(),
      getMyGroupBookings(memberId.value),
      getMyWaitingQueues(memberId.value),
      getMyAbsenceRecords(memberId.value),
    ])
    packages.value = pkg
    courses.value = courseData
    bookings.value = bookingData
    waitingQueues.value = waitingData
    absenceRecords.value = absenceData

    if (!selectedPackageId.value && usablePackages.value.length > 0) {
      selectedPackageId.value = usablePackages.value[0]!.packageId
    }
  } catch (err) {
    error.value = err instanceof Error ? err.message : '加载失败'
  } finally {
    loading.value = false
  }
}

function packageTitle(pkg: GroupPackage) {
  return pkg.packageName || `${pkg.courseTypeName}团课课包·${pkg.totalCount}次`
}

async function handleBook() {
  if (!memberId.value || !selectedPackageId.value || !selectedCourseId.value || !selectedCourseDate.value) {
    messageType.value = 'error'
    message.value = '请选择课包、团课和上课日期'
    return
  }

  bookingBusy.value = true
  message.value = ''
  try {
    const result = await bookGroupCourse({
      memberId: memberId.value,
      packageId: selectedPackageId.value,
      courseId: selectedCourseId.value,
      courseDate: selectedCourseDate.value,
    })
    messageType.value = 'success'
    message.value = result.message || '预约成功，已扣除 1 次'
    selectedCourseDate.value = null
    await loadAll()
  } catch (err) {
    messageType.value = 'error'
    message.value = err instanceof ApiError ? err.message : '预约失败'
  } finally {
    bookingBusy.value = false
  }
}

async function handleCancel(booking: GroupCourseBooking) {
  if (!memberId.value || !booking.canCancel) {
    messageType.value = 'error'
    message.value = booking.canCancel === false ? '开课前三小时内不可取消' : '无法取消'
    return
  }
  if (!window.confirm(`确定取消「${booking.courseName}」？将归还课包 1 次。`)) return

  cancellingId.value = booking.bookingId
  try {
    await cancelGroupCourseById(booking.bookingId, memberId.value)
    messageType.value = 'success'
    message.value = '已取消并归还次数'
    await loadAll()
  } catch (err) {
    messageType.value = 'error'
    message.value = err instanceof ApiError ? err.message : '取消失败'
  } finally {
    cancellingId.value = null
  }
}

function formatTime(value?: string | null) {
  if (!value) return '暂无'
  const d = new Date(value)
  return Number.isNaN(d.getTime()) ? value : d.toLocaleString('zh-CN')
}

onMounted(loadAll)
</script>

<template>
  <div class="group-booking-page">
    <PageHeader
      eyebrow="MY GROUP CLASSES"
      title="我的团课"
      subtitle="您可查看已购课包；预约将提前扣次；开课前三小时可取消，未消耗次数将归还。"
    >
      <template #actions>
        <RouterLink class="ghost-link" to="/member/group-courses">去购买课包</RouterLink>
      </template>
    </PageHeader>

    <p v-if="message" class="notice" :class="messageType">{{ message }}</p>
    <StateCard v-if="loading" message="加载中..." />
    <StateCard v-else-if="error" :message="error" type="error" />

    <template v-else>
      <section class="panel panel-tone-blue">
        <h2>我的课包</h2>
        <StateCard v-if="packages.length === 0" message="还没有团课课包，请先到团课预约页购买。" />
        <div v-else class="pkg-list">
          <article v-for="pkg in packages" :key="pkg.packageId" class="pkg-card" :class="{ usable: pkg.isUsable }">
            <h3>{{ packageTitle(pkg) }}</h3>
            <p>剩余 {{ pkg.remainingCount }} / {{ pkg.totalCount }} · {{ pkg.packageStatusLabel }}</p>
          </article>
        </div>
      </section>

      <section class="panel">
        <h2>用课包预约</h2>
        <StateCard v-if="usablePackages.length === 0" message="没有可用课包，无法预约。" />
        <form v-else class="book-form" @submit.prevent="handleBook">
          <label>
            <span>选择课包</span>
            <select v-model.number="selectedPackageId">
              <option v-for="pkg in usablePackages" :key="pkg.packageId" :value="pkg.packageId">
                {{ packageTitle(pkg) }}（剩余 {{ pkg.remainingCount }}）
              </option>
            </select>
          </label>

          <label>
            <span>选择团课</span>
            <select v-model.number="selectedCourseId">
              <option :value="null" disabled>请选择团课</option>
              <option v-for="c in bookableCourses" :key="c.courseId" :value="c.courseId">
                {{ c.courseName }}（{{ c.currentCapacity }}/{{ c.maxCapacity }}）
              </option>
            </select>
          </label>

          <label>
            <span>选择上课日期</span>
            <select v-model="selectedCourseDate" :disabled="!selectedCourseId">
              <option :value="null" disabled>请选择日期（一个月内对应星期）</option>
              <option v-for="d in availableDates" :key="d.value" :value="d.value">
                {{ d.label }}
              </option>
            </select>
          </label>

          <p v-if="selectedCourseId && availableDates.length === 0" class="form-tip">
            该团课在未来一个月内暂无可预约日期（可能未排期、已满或均已过期）。
          </p>

          <button type="submit" :disabled="bookingBusy || !selectedCourseId || !selectedCourseDate">
            {{ bookingBusy ? '预约中...' : '确认预约（扣 1 次）' }}
          </button>
        </form>
      </section>

      <section class="panel panel-tone-green">
        <h2>我的预约记录</h2>
        <StateCard v-if="bookings.length === 0" message="暂无预约记录。" />
        <div v-else class="booking-list">
          <article v-for="b in bookings" :key="b.bookingId" class="booking-card">
            <div class="row">
              <h3>{{ b.courseName }}</h3>
              <span>{{ b.bookingStatusLabel || b.bookingStatus }}</span>
            </div>
            <p>预约时间：{{ formatTime(b.bookingTime) }}</p>
            <p v-if="b.courseStartTime">开课：{{ formatTime(b.courseStartTime) }}</p>
            <button
              v-if="b.bookingStatus === '1'"
              type="button"
              :disabled="!b.canCancel || cancellingId === b.bookingId"
              @click="handleCancel(b)"
            >
              {{ !b.canCancel ? '临近开课不可取消' : cancellingId === b.bookingId ? '取消中...' : '取消预约' }}
            </button>
          </article>
        </div>
      </section>

      <section v-if="waitingQueues.length" class="panel panel-tone-blue">
        <h2>候补中</h2>
        <article v-for="q in waitingQueues" :key="q.queueId" class="booking-card">
          <h3>{{ q.courseName || `课程 #${q.courseId}` }}</h3>
          <p>加入时间：{{ formatTime(q.enqueueTime) }}</p>
        </article>
      </section>

      <section v-if="absenceRecords.length" class="panel panel-tone-green">
        <h2>爽约记录</h2>
        <article v-for="a in absenceRecords" :key="a.absenceId" class="booking-card">
          <h3>{{ a.courseName }}</h3>
          <p>{{ formatTime(a.absenceTime) }}</p>
        </article>
      </section>
    </template>
  </div>
</template>

<style scoped>
.group-booking-page { max-width: 960px; }
.ghost-link {
  display: inline-flex; padding: 10px 16px; border-radius: 999px; border: 1px solid rgba(42, 67, 101, 0.18);
  color: #2a4365; text-decoration: none; font-weight: 600; background: #e8f1f9;
}
.notice { padding: 12px 16px; border-radius: 20px; margin-bottom: 16px; font-weight: 600; }
.notice.success { background: #e8f4f5; color: #2a4365; }
.notice.error { background: #fcebed; color: #dc2626; }
.panel {
  margin-bottom: 20px; padding: 20px; border-radius: 28px;
}
.panel:not(.panel-tone-blue):not(.panel-tone-green) {
  background: #eef4fc; box-shadow: var(--tj-member-lift); border: var(--tj-member-edge);
}
.panel h2 { margin: 0 0 14px; }
.pkg-list, .booking-list { display: grid; gap: 12px; }
.pkg-card, .booking-card {
  padding: 14px; border-radius: 22px;
  /* 背景色由父级 panel-tone-* 提供 */
}
.pkg-card.usable { outline: 1px solid rgba(42, 67, 101, 0.18); }
.book-form { display: grid; gap: 12px; max-width: 520px; }
.book-form label { display: grid; gap: 6px; }
.book-form select, .book-form button, .booking-card button {
  padding: 10px 12px; border-radius: 999px; border: 1px solid rgba(42, 67, 101, 0.18);
}
.book-form button, .booking-card button {
  border: none; background: #2a4365; color: #fff; font-weight: 600; cursor: pointer;
}
.book-form button:disabled, .booking-card button:disabled { opacity: 0.6; cursor: not-allowed; }
.form-tip { margin: 0; color: #b45309; font-size: 13px; }
.row { display: flex; justify-content: space-between; gap: 12px; align-items: center; }
</style>

