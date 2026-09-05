<script setup lang="ts">
import { onMounted, ref } from 'vue'
import PageHeader from '../../components/ui/PageHeader.vue'
import StateCard from '../../components/ui/StateCard.vue'
import {
  getGroupCourses,
  type GroupCourse,
} from '../../api/groupCourses'
import { bookGroupCourse } from '../../api/groupCourseBookings'
import { useAuthStore } from '../../stores/auth'

const authStore = useAuthStore()

const courses = ref<GroupCourse[]>([])
const loading = ref(true)
const error = ref('')

const bookingCourseId = ref<number | null>(null)
const bookingMessage = ref('')
const bookingError = ref('')

async function loadCourses() {
  loading.value = true
  error.value = ''

  try {
    courses.value = await getGroupCourses()
  } catch (err) {
    error.value =
      err instanceof Error ? err.message : '团课加载失败，请稍后重试'
  } finally {
    loading.value = false
  }
}

function getCapacityText(course: GroupCourse) {
  return `${course.currentCapacity} / ${course.maxCapacity}`
}

function isFull(course: GroupCourse) {
  return course.currentCapacity >= course.maxCapacity
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  })
}

function formatTime(value: string) {
  return new Date(value).toLocaleTimeString('zh-CN', {
    hour: '2-digit',
    minute: '2-digit',
  })
}

async function handleBooking(course: GroupCourse) {
  const memberId = authStore.session?.userId

  if (!memberId) {
    bookingError.value = '未获取到当前会员信息，请先登录'
    bookingMessage.value = ''
    return
  }

  if (isFull(course)) {
    bookingError.value = '该课程已满，无法预约'
    bookingMessage.value = ''
    return
  }

  bookingCourseId.value = course.courseId
  bookingMessage.value = ''
  bookingError.value = ''

  try {
    const result = await bookGroupCourse({
      memberId,
      courseId: course.courseId,
    })

    bookingMessage.value = result.message || '预约成功'

    await loadCourses()
  } catch (err) {
    bookingError.value =
      err instanceof Error ? err.message : '预约失败，请稍后重试'
  } finally {
    bookingCourseId.value = null
  }
}

onMounted(() => {
  loadCourses()
})
</script>

<template>
  <div class="group-course-page">
    <PageHeader
      eyebrow="GROUP COURSES"
      title="团课预约"
      subtitle="浏览当前开放的团体课程，选择合适的课程进行预约。"
    />

    <div
  v-if="bookingMessage"
  class="booking-message success"
>
  {{ bookingMessage }}
</div>

<div
  v-if="bookingError"
  class="booking-message error"
>
  {{ bookingError }}
</div>

    <StateCard
      v-if="loading"
      message="正在加载团课信息..."
    />

    <StateCard
      v-else-if="error"
      :message="error"
      type="error"
    />

    <StateCard
      v-else-if="courses.length === 0"
      message="当前暂无团课安排。"
    />

    <section
      v-else
      class="course-grid"
    >
      <article
        v-for="course in courses"
        :key="course.courseId"
        class="course-card"
      >
        <div class="course-card-header">
          <div>
            <span class="course-type">
              课程类型 {{ course.typeId }}
            </span>

            <h2>{{ course.courseName }}</h2>
          </div>

          <span
            class="status"
            :class="{ full: isFull(course) }"
          >
            {{ isFull(course) ? '已满' : '可预约' }}
          </span>
        </div>

        <p class="summary">
          {{ course.courseSummary || '暂无课程简介' }}
        </p>

        <div class="course-info">
  <div class="info-item">
    <span class="label">课程类型</span>
    <strong>{{ course.courseTypeName }}</strong>
  </div>

  <div class="info-item">
    <span class="label">教练</span>
    <strong>{{ course.coachName }}</strong>
  </div>

  <div class="info-item">
    <span class="label">人数</span>
    <strong>{{ getCapacityText(course) }}</strong>
  </div>
</div>

<div
  v-if="course.timeSlots.length > 0"
  class="course-times"
>
  <span class="label">上课时间</span>

  <div
    v-for="slot in course.timeSlots"
    :key="`${slot.courseDate}-${slot.startTime}`"
    class="course-time"
  >
    {{ formatDate(slot.courseDate) }}
    {{ formatTime(slot.startTime) }}
    -
    {{ formatTime(slot.endTime) }}
  </div>
</div>

<div
  v-else
  class="course-times empty"
>
  上课时间暂未安排
</div>

        <button
  class="booking-button"
  :disabled="isFull(course) || bookingCourseId === course.courseId"
  @click="handleBooking(course)"
>
  {{
    bookingCourseId === course.courseId
      ? '预约中...'
      : isFull(course)
        ? '课程已满'
        : '立即预约'
  }}
</button>
      </article>
    </section>
  </div>
</template>

<style scoped>

.course-times {
  margin-top: 16px;
  padding-top: 16px;
  border-top: 1px solid #e5e7eb;
  color: #334155;
}

.course-times .label {
  display: block;
  margin-bottom: 8px;
  color: #64748b;
}

.course-time {
  line-height: 1.8;
  color: #334155;
}

.course-times.empty {
  color: #64748b;
}

.group-course-page {
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

.course-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 20px;
}

.course-card {
  padding: 22px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
  display: flex;
  flex-direction: column;
  min-height: 300px;
}

.course-card-header {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: flex-start;
}

.course-type {
  display: inline-block;
  margin-bottom: 8px;
  font-size: 12px;
  color: var(--tj-primary);
  font-weight: 600;
}

.course-card h2 {
  margin: 0;
  color: var(--tj-text);
  font-size: 21px;
}

.status {
  flex-shrink: 0;
  padding: 6px 10px;
  border-radius: 999px;
  background: var(--tj-primary-soft);
  color: var(--tj-primary);
  font-size: 12px;
  font-weight: 600;
}

.status.full {
  background: #fcebed;
  color: var(--tj-danger);
}

.summary {
  margin: 18px 0;
  color: var(--tj-text-muted);
  line-height: 1.7;
  min-height: 50px;
}

.course-info {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
  margin-top: auto;
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

.booking-button {
  width: 100%;
  margin-top: 18px;
  padding: 11px 16px;
  border: none;
  border-radius: 10px;
  background: var(--tj-primary);
  color: white;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}

.booking-button:hover:not(:disabled) {
  opacity: 0.9;
}

.booking-button:disabled {
  background: #c8ced9;
  cursor: not-allowed;
}

@media (max-width: 1100px) {
  .course-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 700px) {
  .course-grid {
    grid-template-columns: 1fr;
  }

  .course-info {
    grid-template-columns: 1fr;
  }
}
</style>