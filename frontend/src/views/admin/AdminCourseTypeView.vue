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

const courseTypes = ref<CourseType[]>([])

const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const showForm = ref(false)
const editing = ref(false)

const formTypeId = ref<number | null>(null)
const formTypeName = ref('')

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
    errorMessage.value = '请输入有效的课程类型 ID'
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

onMounted(() => {
  loadCourseTypes()
})
</script>

<template>
  <div class="admin-course-type">
    <PageHeader
      eyebrow="Course Type Management"
      title="课程类型"
      subtitle="维护团课、私教等课程类型的基础信息。"
    >
      <template #actions>
        <button
          class="primary-button"
          type="button"
          @click="openCreateForm"
        >
          新增课程类型
        </button>
      </template>
    </PageHeader>

    <div v-if="successMessage" class="message success">
      {{ successMessage }}
    </div>

    <div v-if="errorMessage" class="message error">
      {{ errorMessage }}
    </div>

    <section class="management-card">
      <div class="card-head">
        <div>
          <p class="card-eyebrow">COURSE TYPES</p>
          <h2>课程类型维护</h2>
        </div>

        <span class="count">
          {{ courseTypes.length }} 个类型
        </span>
      </div>

      <div
        v-if="loading"
        class="empty-state"
      >
        正在加载课程类型……
      </div>

      <div
        v-else-if="courseTypes.length === 0"
        class="empty-state"
      >
        暂无课程类型，请先新增。
      </div>

      <div
        v-else
        class="type-list"
      >
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

    <!-- 新增 / 编辑课程类型弹窗 -->
    <div
      v-if="showForm"
      class="modal-mask"
      @click.self="closeForm"
    >
      <section class="modal-card">
        <div class="modal-head">
          <div>
            <p class="card-eyebrow">
              {{ editing ? 'EDIT COURSE TYPE' : 'CREATE COURSE TYPE' }}
            </p>

            <h2>
              {{ editing ? '修改课程类型' : '新增课程类型' }}
            </h2>
          </div>

          <button
            class="close-button"
            type="button"
            aria-label="关闭"
            @click="closeForm"
          >
            ×
          </button>
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
              placeholder="例如：1"
            />
          </label>

          <label>
            <span>课程类型名称</span>

            <input
              v-model="formTypeName"
              type="text"
              maxlength="100"
              placeholder="例如：瑜伽"
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
    </div>
  </div>
</template>

<style scoped>
.admin-course-type {
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

.management-card h2,
.modal-card h2 {
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
.danger-button,
.close-button {
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
  width: min(560px, 100%);
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
  background: #eef3fb;
  color: var(--tj-text-muted);
  font-size: 24px;
  line-height: 1;
}

.close-button:hover {
  background: #e3eaf5;
}

.type-form {
  display: grid;
  gap: 16px;
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
  margin-top: 4px;
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
</style>