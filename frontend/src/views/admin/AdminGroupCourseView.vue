<script setup lang="ts">
import { onMounted, ref } from 'vue'
import PageHeader from '@/components/ui/PageHeader.vue'
import {
  createCourseType,
  deleteCourseType,
  getCourseTypes,
  updateCourseType,
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

const showForm = ref(false)
const editing = ref(false)

const formTypeId = ref<number | null>(null)
const formTypeName = ref('')

const showCourseForm = ref(false)
const editingCourse = ref(false)

const formCourseId = ref<number | null>(null)
const formCourseName = ref('')
const formMaxCapacity = ref<number | null>(null)
const formCourseSummary = ref('')
const formCourseTypeId = ref<number | null>(null)
const formCoachId = ref<number | null>(null)
const formTimeSlotId = ref('')
const scheduleCourseId = ref<number | null>(null)
const scheduleCoachId = ref<number | null>(null)
const scheduleCourseDate = ref('')
const scheduleStartTime = ref('')
const scheduleEndTime = ref('')

const scheduleLoading = ref(false)
const scheduleResult = ref('')
const scheduleSuccess = ref(false)

function resetScheduleForm() {
  scheduleCourseId.value = null
  scheduleCoachId.value = null
  scheduleCourseDate.value = ''
  scheduleStartTime.value = ''
  scheduleEndTime.value = ''
  scheduleResult.value = ''
  scheduleSuccess.value = false
}

function selectScheduleCourse(course: GroupCourse) {
  scheduleCourseId.value = course.courseId
  scheduleCoachId.value = course.coachId
  scheduleCourseDate.value = ''
  scheduleStartTime.value = ''
  scheduleEndTime.value = ''
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

  if (!scheduleCourseDate.value) {
    scheduleResult.value = '请选择课程日期'
    return
  }

  if (!scheduleStartTime.value || !scheduleEndTime.value) {
    scheduleResult.value = '请选择开始时间和结束时间'
    return
  }

  if (scheduleStartTime.value >= scheduleEndTime.value) {
    scheduleResult.value = '开始时间必须早于结束时间'
    return
  }

  const request: GroupCourseScheduleConflictRequest = {
    coachId: scheduleCoachId.value,
    courseDate: scheduleCourseDate.value,
    startTime: `${scheduleCourseDate.value}T${scheduleStartTime.value}:00`,
    endTime: `${scheduleCourseDate.value}T${scheduleEndTime.value}:00`,
  }

  scheduleLoading.value = true

  try {
    const response = await checkGroupCourseScheduleConflict(
      scheduleCourseId.value,
      request,
    )

    scheduleSuccess.value = true
    scheduleResult.value =
      response?.message ?? '排课无冲突'
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

function openCreateForm() {
  editing.value = false
  formTypeId.value = null
  formTypeName.value = ''
  errorMessage.value = ''
  successMessage.value = ''
  showForm.value = true
}

function openEditForm(courseType: CourseType) {
  editing.value = true
  formTypeId.value = courseType.typeId
  formTypeName.value = courseType.typeName
  errorMessage.value = ''
  successMessage.value = ''
  showForm.value = true
}

function closeForm() {
  showForm.value = false
}

async function submitForm() {
  errorMessage.value = ''
  successMessage.value = ''

  const typeName = formTypeName.value.trim()

  if (!editing.value && (!formTypeId.value || formTypeId.value <= 0)) {
    errorMessage.value = '请输入有效的课程类型ID'
    return
  }

  if (!typeName) {
    errorMessage.value = '请输入课程类型名称'
    return
  }

  try {
    if (editing.value) {
      await updateCourseType(formTypeId.value!, {
        typeId: formTypeId.value!,
        typeName,
      })
      successMessage.value = '课程类型修改成功'
    } else {
      await createCourseType({
        typeId: formTypeId.value!,
        typeName,
      })
      successMessage.value = '课程类型创建成功'
    }

    showForm.value = false
    await loadCourseTypes()
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : '操作失败，请稍后重试'
  }
}

async function removeCourseType(courseType: CourseType) {
  const confirmed = window.confirm(
    `确定要删除课程类型「${courseType.typeName}」吗？`,
  )

  if (!confirmed) {
    return
  }

  errorMessage.value = ''
  successMessage.value = ''

  try {
    await deleteCourseType(courseType.typeId)
    successMessage.value = '课程类型删除成功'
    await loadCourseTypes()
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : '删除失败，请稍后重试'
  }
}

function resetCourseForm() {
  formCourseId.value = null
  formCourseName.value = ''
  formMaxCapacity.value = null
  formCourseSummary.value = ''
  formCourseTypeId.value = null
  formCoachId.value = null
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
  const timeSlotId = formTimeSlotId.value.trim()

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

  if (!timeSlotId) {
    errorMessage.value = '请输入时间模板ID'
    return
  }

  const request: GroupCourseRequest = {
    courseId: formCourseId.value!,
    courseName,
    maxCapacity: formMaxCapacity.value,
    courseSummary: courseSummary || null,
    typeId: formCourseTypeId.value,
    coachId: formCoachId.value,
    timeSlotId,
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
      subtitle="员工维护课程类型，并管理团课基本信息、教练和上课时间。"
    >
      <template #actions>
        <button
          class="primary-button"
          type="button"
          @click="openCreateForm"
        >
          新增课程类型
        </button>

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

    <!-- F9-1 -->
    <section class="management-card">
      <div class="card-head">
        <div>
          <p class="card-eyebrow">F9-1</p>
          <h2>课程类型维护</h2>
        </div>

        <span class="count">
          {{ courseTypes.length }} 个类型
        </span>
      </div>

      <div v-if="loading" class="empty-state">
        正在加载课程类型……
      </div>

      <div
        v-else-if="courseTypes.length === 0"
        class="empty-state"
      >
        暂无课程类型，请先新增。
      </div>

      <div v-else class="type-list">
        <article
          v-for="courseType in courseTypes"
          :key="courseType.typeId"
          class="type-item"
        >
          <div>
            <h3>{{ courseType.typeName }}</h3>
            <p>类型 ID：{{ courseType.typeId }}</p>
          </div>

          <div class="item-actions">
            <button
              class="secondary-button"
              type="button"
              @click="openEditForm(courseType)"
            >
              编辑
            </button>

            <button
              class="danger-button"
              type="button"
              @click="removeCourseType(courseType)"
            >
              删除
            </button>
          </div>
        </article>
      </div>
    </section>

    <!-- F9-1 表单 -->
    <section
      v-if="showForm"
      class="management-card form-card"
    >
      <div class="card-head">
        <div>
          <p class="card-eyebrow">
            {{ editing ? 'F9-1 · EDIT' : 'F9-1 · CREATE' }}
          </p>

          <h2>
            {{ editing ? '修改课程类型' : '新增课程类型' }}
          </h2>
        </div>
      </div>

      <form
        class="type-form"
        @submit.prevent="submitForm"
      >
        <label>
          <span>课程类型 ID</span>

          <input
            v-model.number="formTypeId"
            type="number"
            min="1"
            :disabled="editing"
            placeholder="例如：160004"
          />
        </label>

        <label>
          <span>课程类型名称</span>

          <input
            v-model="formTypeName"
            type="text"
            maxlength="100"
            placeholder="例如：普拉提"
          />
        </label>

        <div class="form-actions">
          <button
            class="secondary-button"
            type="button"
            @click="closeForm"
          >
            取消
          </button>

          <button
            class="primary-button"
            type="submit"
          >
            {{ editing ? '保存修改' : '创建类型' }}
          </button>
        </div>
      </form>
    </section>

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

              <span>
                时间模板：{{ course.timeSlotId }}
              </span>
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

    <!-- F9-2 表单 -->
    <section
      v-if="showCourseForm"
      class="management-card form-card"
    >
      <div class="card-head">
        <div>
          <p class="card-eyebrow">
            {{
              editingCourse
                ? 'F9-2 · EDIT'
                : 'F9-2 · CREATE'
            }}
          </p>

          <h2>
            {{
              editingCourse
                ? '修改团课'
                : '新增团课'
            }}
          </h2>
        </div>
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
          <span>时间模板 ID</span>

          <input
            v-model="formTimeSlotId"
            type="text"
            maxlength="20"
            placeholder="例如：TS001"
          />

          <small class="form-hint">
            F9-2 暂时填写已有时间模板 ID；具体日期、开始时间、
            结束时间以及冲突检测将在 F9-3 完成。
          </small>
        </label>

        <label>
          <span>课程简介</span>

          <textarea
            v-model="formCourseSummary"
            rows="4"
            placeholder="请输入团课简介"
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
            {{
              editingCourse
                ? '保存修改'
                : '创建团课'
            }}
          </button>
        </div>
      </form>
    </section>

    <!-- F9-3 -->
<section class="management-card">
  <div class="card-head">
    <div>
      <p class="card-eyebrow">F9-3</p>
      <h2>团课时间排期与冲突检测</h2>
    </div>

    <span class="count">
      基于教练 + 日期 + 时间段检测
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
        <div>
          <strong>{{ course.courseName }}</strong>

          <span>
            ID：{{ course.courseId }}
          </span>
        </div>

        <small>
          教练：{{ course.coachName }}
        </small>
      </button>
    </div>

    <div class="schedule-form-area">
      <div
        v-if="!scheduleCourseId"
        class="schedule-placeholder"
      >
        <strong>请先选择一门团课</strong>
        <span>
          选择团课后，可以指定上课日期和时间，
          并检测该教练是否存在时间冲突。
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

        <label>
          <span>课程日期</span>

          <input
            v-model="scheduleCourseDate"
            type="date"
          />
        </label>

        <div class="time-row">
          <label>
            <span>开始时间</span>

            <input
              v-model="scheduleStartTime"
              type="time"
            />
          </label>

          <label>
            <span>结束时间</span>

            <input
              v-model="scheduleEndTime"
              type="time"
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
</style>
