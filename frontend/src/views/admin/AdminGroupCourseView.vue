<script setup lang="ts">
import { onMounted, ref } from 'vue'
import PageHeader from '@/components/ui/PageHeader.vue'
import {
  getCourseTypes,
  type CourseType,
} from '@/api/courseTypes'
import {
  checkGroupCourseScheduleConflict,
  createGroupCourse,
  deleteGroupCourse,
  getGroupCourses,
  updateGroupCourse,
  type GroupCourse,
  type GroupCourseRequest,
  type GroupCourseScheduleConflictRequest,
} from '@/api/groupCourses'
import {
  getCoaches,
  type Coach,
} from '@/api/coaches'

const courseTypes = ref<CourseType[]>([])
const groupCourses = ref<GroupCourse[]>([])
const coaches = ref<Coach[]>([])

const loading = ref(false)
const courseLoading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const showCourseForm = ref(false)
const editingCourse = ref(false)

const formCourseId = ref<number | null>(null)
const formCourseName = ref('')
const formMaxCapacity = ref<number | null>(null)
const formCourseSummary = ref('')
const formCourseTypeId = ref<number | null>(null)
const formCoachId = ref<number | null>(null)
const formWeekday = ref<number | null>(null)
const formStartTime = ref('')
const formEndTime = ref('')
const formScheduleFrom = ref('')
const formScheduleTo = ref('')
const formTimeSlotId = ref('')

function defaultScheduleFrom() {
  const d = new Date()
  return d.toISOString().slice(0, 10)
}

function defaultScheduleTo() {
  const d = new Date()
  d.setDate(d.getDate() + 84) // 约 12 周
  return d.toISOString().slice(0, 10)
}

function weekdayFromDate(value?: string | null) {
  if (!value) return null
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return null
  // JS: 0=周日 → 业务 7；1=周一 → 1
  const js = d.getDay()
  return js === 0 ? 7 : js
}

function toHm(value?: string | null) {
  if (!value) return ''
  const match = String(value).match(/(\d{2}):(\d{2})/)
  return match ? `${match[1]}:${match[2]}` : ''
}
const scheduleCourseId = ref<number | null>(null)
const scheduleCoachId = ref<number | null>(null)
const scheduleRangeStart = ref('')
const scheduleRangeEnd = ref('')

const scheduleLoading = ref(false)
const scheduleResult = ref('')
const scheduleSuccess = ref(false)

function resetScheduleForm() {
  scheduleCourseId.value = null
  scheduleCoachId.value = null
  scheduleRangeStart.value = ''
  scheduleRangeEnd.value = ''
  scheduleResult.value = ''
  scheduleSuccess.value = false
}

function selectScheduleCourse(course: GroupCourse) {
  scheduleCourseId.value = course.courseId
  scheduleCoachId.value = course.coachId
  scheduleRangeStart.value = ''
  scheduleRangeEnd.value = ''
  scheduleResult.value = ''
  scheduleSuccess.value = false
}

async function checkScheduleConflict() {
  scheduleResult.value = ''
  scheduleSuccess.value = false

  if (!scheduleCourseId.value || scheduleCourseId.value <= 0) {
    scheduleResult.value = '请选择要排期的团课'
    return
  }

  if (!scheduleCoachId.value || scheduleCoachId.value <= 0) {
    scheduleResult.value = '请选择授课教练'
    return
  }

  if (!scheduleRangeStart.value || !scheduleRangeEnd.value) {
    scheduleResult.value = '请选择排课日期区间（起止日期）'
    return
  }

  if (scheduleRangeEnd.value < scheduleRangeStart.value) {
    scheduleResult.value = '结束日期不能早于开始日期'
    return
  }

  const request: GroupCourseScheduleConflictRequest = {
    coachId: scheduleCoachId.value,
    rangeStart: scheduleRangeStart.value,
    rangeEnd: scheduleRangeEnd.value,
  }

  scheduleLoading.value = true

  try {
    const response = await checkGroupCourseScheduleConflict(
      scheduleCourseId.value,
      request,
    )

    scheduleSuccess.value = true
    scheduleResult.value = response || '排课无冲突'
  } catch (error) {
    scheduleSuccess.value = false
    scheduleResult.value =
      error instanceof Error
        ? error.message
        : '排课冲突检测失败，请稍后重试'
  } finally {
    scheduleLoading.value = false
  }
}
async function loadCourseTypes() {
  loading.value = true
  errorMessage.value = ''

  try {
    courseTypes.value = await getCourseTypes()
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : '课程类型加载失败'
  } finally {
    loading.value = false
  }
}
async function loadGroupCourses() {
  courseLoading.value = true
  errorMessage.value = ''

  try {
    groupCourses.value = await getGroupCourses()
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : '团课加载失败'
  } finally {
    courseLoading.value = false
  }
}

async function loadCoaches() {
  try {
    coaches.value = await getCoaches()
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : '教练加载失败'
  }
}

function formatTime(value: string) {
  if (!value) {
    return ''
  }

  const match = value.match(/(\d{2}):(\d{2})/)
  return match ? `${match[1]}:${match[2]}` : value
}

function getWeekdayLabel(value?: string | null) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const labels = ['周日', '周一', '周二', '周三', '周四', '周五', '周六']
  return labels[date.getDay()] ?? ''
}

function getCourseTimeLabel(course: GroupCourse) {
  const slot = course.timeSlots?.[0]

  if (!slot) {
    return '暂无具体时间'
  }

  const weekday = getWeekdayLabel(slot.courseDate)
  const timeRange = `${formatTime(slot.startTime)} - ${formatTime(slot.endTime)}`
  return weekday ? `${weekday} ${timeRange}` : timeRange
}

function resetCourseForm() {
  formCourseId.value = null
  formCourseName.value = ''
  formMaxCapacity.value = null
  formCourseSummary.value = ''
  formCourseTypeId.value = null
  formCoachId.value = null
  formWeekday.value = 1
  formStartTime.value = '18:00'
  formEndTime.value = '19:00'
  formScheduleFrom.value = defaultScheduleFrom()
  formScheduleTo.value = defaultScheduleTo()
  formTimeSlotId.value = ''
}

function openCreateCourseForm() {
  editingCourse.value = false
  resetCourseForm()
  errorMessage.value = ''
  successMessage.value = ''
  showCourseForm.value = true
}

function openEditCourseForm(course: GroupCourse) {
  editingCourse.value = true

  formCourseId.value = course.courseId
  formCourseName.value = course.courseName
  formMaxCapacity.value = course.maxCapacity
  formCourseSummary.value = course.courseSummary ?? ''
  formCourseTypeId.value = course.typeId
  formCoachId.value = course.coachId
  formTimeSlotId.value = course.timeSlotId

  const slot = course.timeSlots?.[0]
  formWeekday.value = weekdayFromDate(slot?.courseDate) ?? 1
  formStartTime.value = toHm(slot?.startTime) || '18:00'
  formEndTime.value = toHm(slot?.endTime) || '19:00'
  formScheduleFrom.value = defaultScheduleFrom()
  formScheduleTo.value = defaultScheduleTo()

  errorMessage.value = ''
  successMessage.value = ''
  showCourseForm.value = true
}

function closeCourseForm() {
  showCourseForm.value = false
}

async function submitCourseForm() {
  errorMessage.value = ''
  successMessage.value = ''

  const courseName = formCourseName.value.trim()
  const courseSummary = formCourseSummary.value.trim()

  if (
    !editingCourse.value &&
    (!formCourseId.value || formCourseId.value <= 0)
  ) {
    errorMessage.value = '请输入有效的团课ID'
    return
  }

  if (!courseName) {
    errorMessage.value = '请输入团课名称'
    return
  }

  if (
    !formMaxCapacity.value ||
    formMaxCapacity.value <= 0
  ) {
    errorMessage.value = '请输入有效的最大容量'
    return
  }

  if (!formCourseTypeId.value || formCourseTypeId.value <= 0) {
    errorMessage.value = '请选择课程类型'
    return
  }

  if (!formCoachId.value || formCoachId.value <= 0) {
    errorMessage.value = '请选择授课教练'
    return
  }

  if (!formWeekday.value || formWeekday.value < 1 || formWeekday.value > 7) {
    errorMessage.value = '请选择上课星期'
    return
  }

  if (!formStartTime.value || !formEndTime.value) {
    errorMessage.value = '请选择开始时间和结束时间'
    return
  }

  if (formStartTime.value >= formEndTime.value) {
    errorMessage.value = '结束时间必须晚于开始时间'
    return
  }

  if (!formScheduleFrom.value || !formScheduleTo.value) {
    errorMessage.value = '请选择排期起止日期（用于生成每周上课日）'
    return
  }

  if (formScheduleTo.value < formScheduleFrom.value) {
    errorMessage.value = '排期结束日期不能早于开始日期'
    return
  }

  const request: GroupCourseRequest = {
    courseId: formCourseId.value!,
    courseName,
    maxCapacity: formMaxCapacity.value,
    courseSummary: courseSummary || null,
    typeId: formCourseTypeId.value,
    coachId: formCoachId.value,
    weekday: formWeekday.value,
    startTime: formStartTime.value,
    endTime: formEndTime.value,
    scheduleFrom: formScheduleFrom.value,
    scheduleTo: formScheduleTo.value,
    timeSlotId: formTimeSlotId.value || undefined,
  }

  try {
    if (editingCourse.value) {
      await updateGroupCourse(
        formCourseId.value!,
        request,
      )
      successMessage.value = '团课修改成功'
    } else {
      await createGroupCourse(request)
      successMessage.value = '团课创建成功'
    }

    showCourseForm.value = false
    await loadGroupCourses()
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : '团课操作失败，请稍后重试'
  }
}

async function removeGroupCourse(course: GroupCourse) {
  const confirmed = window.confirm(
    `确定要删除团课「${course.courseName}」吗？`,
  )

  if (!confirmed) {
    return
  }

  errorMessage.value = ''
  successMessage.value = ''

  try {
    await deleteGroupCourse(course.courseId)
    successMessage.value = '团课删除成功'
    await loadGroupCourses()
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : '团课删除失败，请稍后重试'
  }
}

onMounted(async () => {
  await Promise.all([
    loadCourseTypes(),
    loadGroupCourses(),
    loadCoaches(),
  ])
})
</script>

<template>
  <div class="admin-group-course">
    <PageHeader
      eyebrow="Group Course Management"
      title="团课排期管理"
      subtitle="维护团课基本信息、授课教练、容量和上课时间。"
    >
      <template #actions>

        <button
          class="primary-button"
          type="button"
          @click="openCreateCourseForm"
        >
          新增团课
        </button>
      </template>
    </PageHeader>

    <div v-if="successMessage" class="message success">
      {{ successMessage }}
    </div>

    <div v-if="errorMessage" class="message error">
      {{ errorMessage }}
    </div>

    <!-- F9-2 -->
    <section class="management-card">
      <div class="card-head">
        <div>
          <p class="card-eyebrow">F9-2</p>
          <h2>团课基本信息</h2>
        </div>

        <span class="count">
          {{ groupCourses.length }} 门团课
        </span>
      </div>

      <div
        v-if="courseLoading"
        class="empty-state"
      >
        正在加载团课……
      </div>

      <div
        v-else-if="groupCourses.length === 0"
        class="empty-state"
      >
        暂无团课，请先新增。
      </div>

      <div v-else class="course-list">
        <article
          v-for="course in groupCourses"
          :key="course.courseId"
          class="course-item"
        >
          <div class="course-main">
            <div class="course-title-row">
              <h3>{{ course.courseName }}</h3>

              <span class="course-id">
                ID：{{ course.courseId }}
              </span>
            </div>

            <div class="course-meta">
              <span>
                类型：{{ course.courseTypeName }}
              </span>

              <span>
                教练：{{ course.coachName }}
              </span>

              <span>
                容量：
                {{ course.currentCapacity }}
                /
                {{ course.maxCapacity }}
              </span>

              <p>
  上课时间：{{ getCourseTimeLabel(course) }}
</p>
            </div>

            <p
              v-if="course.courseSummary"
              class="course-summary"
            >
              {{ course.courseSummary }}
            </p>
          </div>

          <div class="item-actions">
            <button
              class="secondary-button"
              type="button"
              @click="openEditCourseForm(course)"
            >
              编辑
            </button>

            <button
              class="danger-button"
              type="button"
              @click="removeGroupCourse(course)"
            >
              删除
            </button>
          </div>
        </article>
      </div>
    </section>

    <!-- F9-2 新增 / 编辑团课弹窗 -->
<div
  v-if="showCourseForm"
  class="modal-mask"
  @click.self="closeCourseForm"
>
  <section class="modal-card">
    <div class="modal-head">
      <div>
        <p class="card-eyebrow">
          {{ editingCourse ? 'F9-2 · EDIT' : 'F9-2 · CREATE' }}
        </p>

        <h2>
          {{ editingCourse ? '修改团课' : '新增团课' }}
        </h2>
      </div>

      <button
        class="close-button"
        type="button"
        aria-label="关闭"
        @click="closeCourseForm"
      >
        ×
      </button>
    </div>

    <form
      class="course-form"
      @submit.prevent="submitCourseForm"
    >
      <label>
        <span>团课 ID</span>

        <input
          v-model.number="formCourseId"
          type="number"
          min="1"
          :disabled="editingCourse"
          placeholder="例如：100001"
        />
      </label>

      <label>
        <span>团课名称</span>

        <input
          v-model="formCourseName"
          type="text"
          maxlength="100"
          placeholder="例如：瑜伽基础"
        />
      </label>

      <label>
        <span>课程类型</span>

        <select v-model.number="formCourseTypeId">
          <option :value="null">
            请选择课程类型
          </option>

          <option
            v-for="courseType in courseTypes"
            :key="courseType.typeId"
            :value="courseType.typeId"
          >
            {{ courseType.typeName }}
          </option>
        </select>
      </label>

      <label>
        <span>授课教练</span>

        <select v-model.number="formCoachId">
          <option :value="null">
            请选择授课教练
          </option>

          <option
            v-for="coach in coaches"
            :key="coach.coachId"
            :value="coach.coachId"
          >
            {{ coach.coachName }}
            <template v-if="coach.specialty">
              · {{ coach.specialty }}
            </template>
          </option>
        </select>
      </label>

      <label>
        <span>最大容量</span>

        <input
          v-model.number="formMaxCapacity"
          type="number"
          min="1"
          max="32767"
          placeholder="例如：30"
        />
      </label>

      <label>
        <span>当前报名人数</span>

        <input
          value="新增时自动为 0"
          type="text"
          disabled
        />

        <small class="form-hint">
          当前报名人数由预约业务自动维护，管理员不能直接修改。
        </small>
      </label>

      <label>
        <span>上课星期</span>
        <select v-model.number="formWeekday">
          <option :value="1">周一</option>
          <option :value="2">周二</option>
          <option :value="3">周三</option>
          <option :value="4">周四</option>
          <option :value="5">周五</option>
          <option :value="6">周六</option>
          <option :value="7">周日</option>
        </select>
      </label>

      <div class="time-row">
        <label>
          <span>开始时间</span>
          <input v-model="formStartTime" type="time" />
        </label>
        <label>
          <span>结束时间</span>
          <input v-model="formEndTime" type="time" />
        </label>
      </div>

      <div class="time-row">
        <label>
          <span>排期起始日</span>
          <input v-model="formScheduleFrom" type="date" />
        </label>
        <label>
          <span>排期结束日</span>
          <input v-model="formScheduleTo" type="date" />
        </label>
      </div>

      <small class="form-hint">
        按周排课：系统会写入时间模板，并在起止日期内为每个对应星期几生成上课日（本周会员端依赖这些日期）。
      </small>

      <label>
        <span>课程描述</span>

        <textarea
          v-model="formCourseSummary"
          rows="4"
          maxlength="1000"
          placeholder="请输入课程简介、适合人群等信息"
        />
      </label>

      <div class="form-actions">
        <button
          class="secondary-button"
          type="button"
          @click="closeCourseForm"
        >
          取消
        </button>

        <button
          class="primary-button"
          type="submit"
        >
          {{ editingCourse ? '保存修改' : '创建团课' }}
        </button>
      </div>
    </form>
  </section>
</div>

    <!-- F9-3 -->
<section class="management-card">
  <div class="card-head">
    <div>
      <p class="card-eyebrow">F9-3</p>
      <h2>团课时间排期与冲突检测</h2>
    </div>

    <span class="count">
      按周课模式：教练 + 日期区间
    </span>
  </div>

  <div class="schedule-layout">
    <div class="schedule-course-list">
      <div class="schedule-section-title">
        <strong>选择团课</strong>
        <span>共 {{ groupCourses.length }} 门</span>
      </div>

      <div
        v-if="groupCourses.length === 0"
        class="empty-state"
      >
        暂无团课，请先创建团课。
      </div>

      <button
        v-for="course in groupCourses"
        :key="course.courseId"
        class="schedule-course-item"
        :class="{
          selected: scheduleCourseId === course.courseId,
        }"
        type="button"
        @click="selectScheduleCourse(course)"
      >
        <strong>{{ course.courseName }}</strong>
      </button>
    </div>

    <div class="schedule-form-area">
      <div
        v-if="!scheduleCourseId"
        class="schedule-placeholder"
      >
        <strong>请先选择一门团课</strong>
        <span>
          选择团课后，指定意向授课教练与日期区间，
          系统按该课的「星期几 + 时段」在区间内逐周检测冲突。
        </span>
      </div>

      <form
        v-else
        class="schedule-form"
        @submit.prevent="checkScheduleConflict"
      >
        <div class="selected-course">
          <span>当前排期课程</span>
          <strong>
            {{
              groupCourses.find(
                course => course.courseId === scheduleCourseId,
              )?.courseName
            }}
          </strong>
        </div>

        <label>
          <span>授课教练</span>

          <select
            v-model.number="scheduleCoachId"
          >
            <option :value="null">
              请选择授课教练
            </option>

            <option
              v-for="coach in coaches"
              :key="coach.coachId"
              :value="coach.coachId"
            >
              {{ coach.coachName }}
              <template v-if="coach.specialty">
                · {{ coach.specialty }}
              </template>
            </option>
          </select>
        </label>

        <div class="time-row">
          <label>
            <span>起始日期</span>
            <input
              v-model="scheduleRangeStart"
              type="date"
            />
          </label>

          <label>
            <span>结束日期</span>
            <input
              v-model="scheduleRangeEnd"
              type="date"
            />
          </label>
        </div>

        <div
          v-if="scheduleResult"
          class="schedule-result"
          :class="{
            success: scheduleSuccess,
            error: !scheduleSuccess,
          }"
        >
          {{ scheduleResult }}
        </div>

        <div class="form-actions">
          <button
            class="secondary-button"
            type="button"
            @click="resetScheduleForm"
          >
            清空
          </button>

          <button
            class="primary-button"
            type="submit"
            :disabled="scheduleLoading"
          >
            {{
              scheduleLoading
                ? '正在检测……'
                : '检测排课冲突'
            }}
          </button>
        </div>
      </form>
    </div>
  </div>
</section>
  </div>
</template>

<style scoped>
.admin-group-course {
  display: grid;
  gap: 20px;
}

.management-card {
  padding: 22px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.card-head {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: flex-start;
  margin-bottom: 18px;
}

.card-eyebrow {
  margin: 0 0 6px;
  color: #4d77ff;
  font-size: 12px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.management-card h2 {
  margin: 0;
  color: var(--tj-text);
  font-size: 22px;
}

.count {
  color: var(--tj-text-muted);
  font-size: 13px;
}

.type-list {
  display: grid;
  gap: 10px;
}

.type-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 14px;
  border: 1px solid #e6edf8;
  border-radius: 14px;
  background: #f8fbff;
}

.type-item h3 {
  margin: 0;
  font-size: 17px;
  color: var(--tj-text);
}

.type-item p {
  margin: 5px 0 0;
  color: var(--tj-text-muted);
  font-size: 13px;
}

.item-actions,
.form-actions {
  display: flex;
  gap: 8px;
}

.primary-button,
.secondary-button,
.danger-button {
  border: 0;
  border-radius: 10px;
  padding: 9px 13px;
  cursor: pointer;
  font: inherit;
  font-weight: 600;
}

.primary-button {
  background: #285cff;
  color: #fff;
}

.secondary-button {
  background: #eef3fb;
  color: #285cff;
}

.danger-button {
  background: #fde8ea;
  color: #b42318;
}

.primary-button:disabled,
.secondary-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.message {
  padding: 11px 14px;
  border-radius: 10px;
  font-size: 13px;
}

.success {
  background: #e8f7ef;
  color: #137333;
}

.error {
  background: #fde8ea;
  color: #b42318;
}

.empty-state {
  padding: 28px;
  text-align: center;
  color: var(--tj-text-muted);
}

.type-form {
  display: grid;
  gap: 16px;
  max-width: 560px;
}

.type-form label {
  display: grid;
  gap: 7px;
}

.type-form label span {
  color: var(--tj-text);
  font-size: 13px;
  font-weight: 600;
}

.type-form input {
  width: 100%;
  box-sizing: border-box;
  padding: 10px 12px;
  border: 1px solid #d9e2ef;
  border-radius: 10px;
  background: #fff;
  color: var(--tj-text);
  font: inherit;
}

.type-form input:focus {
  outline: 2px solid #dbe4ff;
  border-color: #285cff;
}

.form-actions {
  justify-content: flex-end;
}

.next-card p:last-child {
  margin: 8px 0 0;
  color: var(--tj-text-muted);
  line-height: 1.6;
}

.course-list {
  display: grid;
  gap: 10px;
}

.course-item {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 20px;
  padding: 16px;
  border: 1px solid #e6edf8;
  border-radius: 14px;
  background: #f8fbff;
}

.course-main {
  min-width: 0;
  flex: 1;
}

.course-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.course-title-row h3 {
  margin: 0;
  color: var(--tj-text);
  font-size: 18px;
}

.course-id {
  color: var(--tj-text-muted);
  font-size: 12px;
}

.course-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 18px;
  margin-top: 9px;
  color: var(--tj-text-muted);
  font-size: 13px;
}

.course-summary {
  margin: 10px 0 0;
  color: var(--tj-text-muted);
  line-height: 1.6;
  font-size: 13px;
}

.course-form {
  display: grid;
  gap: 16px;
  max-width: 680px;
}

.course-form label {
  display: grid;
  gap: 7px;
}

.course-form label > span {
  color: var(--tj-text);
  font-size: 13px;
  font-weight: 600;
}

.course-form input,
.course-form select,
.course-form textarea {
  width: 100%;
  box-sizing: border-box;
  padding: 10px 12px;
  border: 1px solid #d9e2ef;
  border-radius: 10px;
  background: #fff;
  color: var(--tj-text);
  font: inherit;
}

.course-form textarea {
  resize: vertical;
  min-height: 100px;
}

.course-form input:focus,
.course-form select:focus,
.course-form textarea:focus {
  outline: 2px solid #dbe4ff;
  border-color: #285cff;
}

.form-hint {
  color: var(--tj-text-muted);
  font-size: 12px;
  line-height: 1.5;
}

@media (max-width: 700px) {
  .course-item {
    flex-direction: column;
  }

  .course-item .item-actions {
    width: 100%;
  }
}

@media (max-width: 700px) {
  .type-item {
    align-items: flex-start;
    flex-direction: column;
  }

  .item-actions {
    width: 100%;
  }
}
.schedule-layout {
  display: grid;
  grid-template-columns: minmax(260px, 0.8fr) minmax(360px, 1.2fr);
  gap: 20px;
}

.schedule-course-list {
  display: grid;
  gap: 10px;
  align-content: start;
}

.schedule-section-title {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 4px;
  color: var(--tj-text);
  font-size: 14px;
}

.schedule-section-title span {
  color: var(--tj-text-muted);
  font-size: 12px;
}

.schedule-course-item {
  width: 100%;
  padding: 14px;
  border: 1px solid #e6edf8;
  border-radius: 12px;
  background: #f8fbff;
  color: var(--tj-text);
  text-align: left;
  cursor: pointer;
  font: inherit;
}

.schedule-course-item:hover {
  border-color: #b9c9f7;
}

.schedule-course-item.selected {
  border-color: #285cff;
  background: #eef3ff;
}

.schedule-course-item > div {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.schedule-course-item strong {
  font-size: 15px;
}

.schedule-course-item span,
.schedule-course-item small {
  color: var(--tj-text-muted);
  font-size: 12px;
}

.schedule-course-item small {
  display: block;
  margin-top: 6px;
}

.schedule-form-area {
  min-width: 0;
}

.schedule-form {
  display: grid;
  gap: 16px;
}

.schedule-form label {
  display: grid;
  gap: 7px;
}

.schedule-form label > span {
  color: var(--tj-text);
  font-size: 13px;
  font-weight: 600;
}

.schedule-form input,
.schedule-form select {
  width: 100%;
  box-sizing: border-box;
  padding: 10px 12px;
  border: 1px solid #d9e2ef;
  border-radius: 10px;
  background: #fff;
  color: var(--tj-text);
  font: inherit;
}

.schedule-form input:focus,
.schedule-form select:focus {
  outline: 2px solid #dbe4ff;
  border-color: #285cff;
}

.time-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}

.selected-course {
  display: grid;
  gap: 5px;
  padding: 14px;
  border-radius: 12px;
  background: #f3f6fc;
}

.selected-course span {
  color: var(--tj-text-muted);
  font-size: 12px;
}

.selected-course strong {
  color: var(--tj-text);
  font-size: 16px;
}

.schedule-placeholder {
  min-height: 220px;
  display: grid;
  place-content: center;
  gap: 8px;
  padding: 20px;
  border: 1px dashed #d5deec;
  border-radius: 14px;
  text-align: center;
}

.schedule-placeholder strong {
  color: var(--tj-text);
}

.schedule-placeholder span {
  color: var(--tj-text-muted);
  font-size: 13px;
}

.schedule-result {
  padding: 12px 14px;
  border-radius: 10px;
  font-size: 13px;
  line-height: 1.5;
}

.schedule-result.success {
  background: #e8f7ef;
  color: #137333;
}

.schedule-result.error {
  background: #fde8ea;
  color: #b42318;
}

@media (max-width: 800px) {
  .schedule-layout {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 500px) {
  .time-row {
    grid-template-columns: 1fr;
  }
}

.modal-mask {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: grid;
  place-items: center;
  padding: 20px;
  background: rgba(15, 23, 42, 0.48);
}

.modal-card {
  width: min(680px, 100%);
  max-height: calc(100vh - 40px);
  overflow-y: auto;
  padding: 24px;
  border-radius: 18px;
  background: var(--tj-card-bg);
  box-shadow: 0 20px 60px rgba(15, 23, 42, 0.25);
}

.modal-head {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: flex-start;
  margin-bottom: 22px;
}

.close-button {
  width: 36px;
  height: 36px;
  padding: 0;
  border: 0;
  border-radius: 10px;
  background: #eef3fb;
  color: var(--tj-text-muted);
  cursor: pointer;
  font-size: 24px;
  line-height: 1;
}

.close-button:hover {
  background: #e3eaf5;
}

.course-form {
  display: grid;
  gap: 16px;
}

.course-form label {
  display: grid;
  gap: 7px;
}

.course-form label > span {
  color: var(--tj-text);
  font-size: 13px;
  font-weight: 600;
}

.course-form input,
.course-form select,
.course-form textarea {
  width: 100%;
  box-sizing: border-box;
  padding: 10px 12px;
  border: 1px solid #d9e2ef;
  border-radius: 10px;
  background: #fff;
  color: var(--tj-text);
  font: inherit;
}

.course-form textarea {
  resize: vertical;
  min-height: 100px;
}

.course-form input:focus,
.course-form select:focus,
.course-form textarea:focus {
  outline: 2px solid #dbe4ff;
  border-color: #285cff;
}

.form-hint {
  color: var(--tj-text-muted);
  font-size: 12px;
  line-height: 1.5;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 4px;
}

@media (max-width: 700px) {
  .modal-card {
    padding: 18px;
  }
}

</style>
