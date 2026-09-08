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
  createGroupPackageProduct,
  getManageGroupPackageProducts,
  patchGroupPackageProduct,
  type GroupPackageProduct,
} from '@/api/group-packages'
import { ApiError } from '@/api/http'

const courseTypes = ref<CourseType[]>([])
const packageProducts = ref<GroupPackageProduct[]>([])

const loading = ref(false)
const pkgLoading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const showForm = ref(false)
const editing = ref(false)

const formTypeId = ref<number | null>(null)
const formTypeName = ref('')

const showPkgForm = ref(false)
const pkgTypeId = ref<number | null>(null)
const pkgSessionCount = ref(10)
const pkgPrice = ref(199)
const pkgBusy = ref(false)
const togglingPriceId = ref<number | null>(null)

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

async function loadPackageProducts() {
  pkgLoading.value = true
  try {
    packageProducts.value = await getManageGroupPackageProducts()
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : '课包商品加载失败'
  } finally {
    pkgLoading.value = false
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

function openPkgForm() {
  pkgTypeId.value = courseTypes.value[0]?.typeId ?? null
  pkgSessionCount.value = 10
  pkgPrice.value = 199
  showPkgForm.value = true
  errorMessage.value = ''
  successMessage.value = ''
}

function closePkgForm() {
  showPkgForm.value = false
}

async function submitPkgForm() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!pkgTypeId.value || pkgTypeId.value <= 0) {
    errorMessage.value = '请选择课程类型'
    return
  }
  if (!pkgSessionCount.value || pkgSessionCount.value <= 0) {
    errorMessage.value = '请输入有效次数'
    return
  }
  if (pkgPrice.value == null || pkgPrice.value < 0) {
    errorMessage.value = '请输入有效价格'
    return
  }

  pkgBusy.value = true
  try {
    await createGroupPackageProduct({
      typeId: pkgTypeId.value,
      sessionCount: pkgSessionCount.value,
      standardPrice: pkgPrice.value,
    })
    successMessage.value = '团课课包商品已上架'
    showPkgForm.value = false
    await loadPackageProducts()
  } catch (error) {
    errorMessage.value =
      error instanceof ApiError
        ? error.message
        : error instanceof Error
          ? error.message
          : '创建课包商品失败'
  } finally {
    pkgBusy.value = false
  }
}

async function updatePkgPrice(product: GroupPackageProduct) {
  const input = window.prompt(`修改「${product.name}」价格（元）`, String(product.price))
  if (input == null) return
  const next = Number(input)
  if (!Number.isFinite(next) || next < 0) {
    errorMessage.value = '价格无效'
    return
  }

  try {
    await patchGroupPackageProduct(product.priceId, { standardPrice: next })
    successMessage.value = '价格已更新'
    await loadPackageProducts()
  } catch (error) {
    errorMessage.value =
      error instanceof ApiError
        ? error.message
        : error instanceof Error
          ? error.message
          : '改价失败'
  }
}

async function togglePkgActive(product: GroupPackageProduct) {
  const nextActive = !product.isActive
  const tip = nextActive ? '确认重新上架该课包？' : '确认下架该课包？会员端将不可见。'
  if (!window.confirm(tip)) return

  togglingPriceId.value = product.priceId
  try {
    await patchGroupPackageProduct(product.priceId, { isActive: nextActive })
    successMessage.value = nextActive ? '已上架' : '已下架'
    await loadPackageProducts()
  } catch (error) {
    errorMessage.value =
      error instanceof ApiError
        ? error.message
        : error instanceof Error
          ? error.message
          : '状态更新失败'
  } finally {
    togglingPriceId.value = null
  }
}

onMounted(() => {
  void loadCourseTypes()
  void loadPackageProducts()
})
</script>

<template>
  <div class="admin-course-type">
    <PageHeader
      eyebrow="Course Type Management"
      title="课程类型"
      subtitle="维护课程类型，并在此上架/改价/上下架团课课包（按类型核销、按次、无有效期）。"
    >
      <template #actions>
        <button
          class="secondary-button"
          type="button"
          @click="openPkgForm"
        >
          上架团课课包
        </button>
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

    <section class="management-card">
      <div class="card-head">
        <div>
          <p class="card-eyebrow">GROUP PACKAGE PRODUCTS</p>
          <h2>团课课包商品</h2>
        </div>
        <span class="count">{{ packageProducts.length }} 个商品</span>
      </div>

      <div v-if="pkgLoading" class="empty-state">正在加载课包商品……</div>
      <div v-else-if="packageProducts.length === 0" class="empty-state">
        暂无课包商品，请点击「上架团课课包」。
      </div>
      <div v-else class="type-list">
        <article
          v-for="product in packageProducts"
          :key="product.priceId"
          class="type-item"
        >
          <div>
            <h3>{{ product.name }}</h3>
            <p>
              {{ product.courseTypeName }} · {{ product.sessionCount }} 次 ·
              ¥{{ product.price }} ·
              {{ product.isActive ? '在售' : '已下架' }}
            </p>
          </div>
          <div class="item-actions">
            <button
              class="secondary-button"
              type="button"
              @click="updatePkgPrice(product)"
            >
              改价
            </button>
            <button
              class="secondary-button"
              type="button"
              :disabled="togglingPriceId === product.priceId"
              @click="togglePkgActive(product)"
            >
              {{ product.isActive ? '下架' : '上架' }}
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

    <!-- 上架团课课包弹窗 -->
    <div
      v-if="showPkgForm"
      class="modal-mask"
      @click.self="closePkgForm"
    >
      <section class="modal-card">
        <div class="modal-head">
          <div>
            <p class="card-eyebrow">CREATE GROUP PACKAGE</p>
            <h2>上架团课课包</h2>
          </div>
          <button
            class="close-button"
            type="button"
            aria-label="关闭"
            @click="closePkgForm"
          >
            ×
          </button>
        </div>

        <form class="type-form" @submit.prevent="submitPkgForm">
          <label>
            <span>课程类型</span>
            <select v-model.number="pkgTypeId">
              <option
                v-for="t in courseTypes"
                :key="t.typeId"
                :value="t.typeId"
              >
                {{ t.typeName }}（ID {{ t.typeId }}）
              </option>
            </select>
          </label>

          <label>
            <span>次数</span>
            <input
              v-model.number="pkgSessionCount"
              type="number"
              min="1"
              step="1"
            />
          </label>

          <label>
            <span>标准价格（元）</span>
            <input
              v-model.number="pkgPrice"
              type="number"
              min="0"
              step="0.01"
            />
          </label>

          <div class="form-actions">
            <button
              class="secondary-button"
              type="button"
              @click="closePkgForm"
            >
              取消
            </button>
            <button
              class="primary-button"
              type="submit"
              :disabled="pkgBusy"
            >
              {{ pkgBusy ? '提交中...' : '上架' }}
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

.type-form select {
  width: 100%;
  box-sizing: border-box;
  padding: 10px 12px;
  border: 1px solid #d9e2ef;
  border-radius: 10px;
  background: #fff;
  color: var(--tj-text);
  font: inherit;
}

.type-form input:focus,
.type-form select:focus {
  outline: 2px solid #dbe4ff;
  border-color: #285cff;
}

.mono {
  margin-top: 4px;
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 12px;
  color: #64748b;
  word-break: break-all;
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