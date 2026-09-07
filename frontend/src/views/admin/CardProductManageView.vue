<script setup lang="ts">
// 员工端卡商品管理页，维护 PRICE_LIST 中的会员卡商品。

import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ApiError } from '@/api/http'
import {
  createCardProduct,
  getManageCardProducts,
  patchCardProduct,
  updateCardProduct,
  type CardProduct,
} from '@/api/card-products'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'

const route = useRoute()
const isPreview = computed(() => route.path.startsWith('/preview/admin'))

const products = ref<CardProduct[]>([])
const loading = ref(true)
const errorMessage = ref('')
const noticeMessage = ref('')
const saving = ref(false)

const createForm = reactive({
  productType: 'MEMBERSHIP_TIME_90',
  standardPrice: 599,
})

const editForm = reactive({
  priceId: 0,
  productType: '',
  standardPrice: 0,
  isActive: true,
})

const isEditing = ref(false)

// 加载管理列表
async function loadProducts() {
  loading.value = true
  errorMessage.value = ''

  try {
    products.value = await getManageCardProducts()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '商品列表加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

// 新增商品
async function handleCreate() {
  if (isPreview.value) {
    errorMessage.value = '预览模式仅展示界面，请登录员工账号后再维护商品。'
    return
  }

  saving.value = true
  noticeMessage.value = ''
  errorMessage.value = ''

  try {
    await createCardProduct({
      productType: createForm.productType.trim(),
      standardPrice: createForm.standardPrice,
    })
    noticeMessage.value = '商品新增成功。'
    await loadProducts()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '新增失败，请稍后重试。'
  } finally {
    saving.value = false
  }
}

// 进入编辑模式
function startEdit(product: CardProduct) {
  editForm.priceId = product.priceId
  editForm.productType = product.productType.replace(/^INACTIVE_/i, '')
  editForm.standardPrice = product.price
  editForm.isActive = product.isActive !== false
  isEditing.value = true
  noticeMessage.value = ''
}

// 取消编辑
function cancelEdit() {
  isEditing.value = false
}

// 保存编辑（全量 PUT）
async function handleSaveEdit() {
  if (isPreview.value) {
    errorMessage.value = '预览模式仅展示界面，请登录员工账号后再维护商品。'
    return
  }

  saving.value = true
  noticeMessage.value = ''
  errorMessage.value = ''

  try {
    await updateCardProduct(editForm.priceId, {
      productType: editForm.productType.trim(),
      standardPrice: editForm.standardPrice,
      isActive: editForm.isActive,
    })
    noticeMessage.value = '商品更新成功。'
    isEditing.value = false
    await loadProducts()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '更新失败，请稍后重试。'
  } finally {
    saving.value = false
  }
}

// 快速上架/下架（PATCH）
async function toggleActive(product: CardProduct) {
  if (isPreview.value) {
    errorMessage.value = '预览模式仅展示界面，请登录员工账号后再维护商品。'
    return
  }

  const nextActive = product.isActive === false
  const actionText = nextActive ? '上架' : '下架'
  const ok = window.confirm(`确认${actionText}商品 #${product.priceId} "${product.name}" ？`)
  if (!ok) {
    return
  }

  saving.value = true
  noticeMessage.value = ''
  errorMessage.value = ''

  try {
    await patchCardProduct(product.priceId, { isActive: nextActive })
    noticeMessage.value = `商品已${actionText}。`
    await loadProducts()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : `${actionText}失败，请稍后重试。`
  } finally {
    saving.value = false
  }
}

onMounted(() => {
  loadProducts()
})
</script>

<template>
  <div class="manage-page">
    <PageHeader
      eyebrow="Admin · Card Products"
      title="卡商品管理"
      subtitle="维护 PRICE_LIST 表中的会员卡商品。下架通过在 PRODUCT_TYPE 前加 INACTIVE_ 前缀实现，无需改表结构。"
    />

    <p v-if="isPreview" class="preview-banner">预览模式：仅展示页面布局与数据，不能新增、编辑或上/下架。请从登录页用员工账号进入。</p>
    <p v-if="noticeMessage" class="notice-banner">{{ noticeMessage }}</p>
    <StateCard v-if="loading" message="商品列表加载中..." />
    <StateCard v-else-if="errorMessage && products.length === 0" :message="errorMessage" type="error" />

    <template v-else>
      <p v-if="errorMessage" class="inline-error">{{ errorMessage }}</p>

      <section class="panel">
        <h2>新增商品</h2>
        <form class="form-grid" @submit.prevent="handleCreate">
          <label>
            商品编码（PRODUCT_TYPE）
            <input v-model="createForm.productType" type="text" placeholder="MEMBERSHIP_TIME_90" />
          </label>
          <label>
            标准价格
            <input v-model.number="createForm.standardPrice" type="number" min="0.01" step="0.01" />
          </label>
          <button type="submit" class="primary-btn" :disabled="saving || isPreview">新增</button>
        </form>
        <p class="hint">示例：MEMBERSHIP_TIME_90（季卡）、MEMBERSHIP_TIME_365（年卡）、MEMBERSHIP_COUNT_20（20次卡）</p>
      </section>

      <section v-if="isEditing" class="panel edit-panel">
        <h2>编辑商品 #{{ editForm.priceId }}</h2>
        <form class="form-grid" @submit.prevent="handleSaveEdit">
          <label>
            商品编码
            <input v-model="editForm.productType" type="text" />
          </label>
          <label>
            标准价格
            <input v-model.number="editForm.standardPrice" type="number" min="0.01" step="0.01" />
          </label>
          <label class="checkbox-row">
            <input v-model="editForm.isActive" type="checkbox" />
            在售
          </label>
          <div class="btn-row">
            <button type="button" class="ghost-btn" @click="cancelEdit">取消</button>
            <button type="submit" class="primary-btn" :disabled="saving || isPreview">保存</button>
          </div>
        </form>
      </section>

      <section class="panel">
        <h2>商品列表</h2>
        <p v-if="products.length === 0" class="empty-text">暂无商品，请先在上方新增。</p>

        <article v-for="product in products" :key="product.priceId" class="product-row">
          <div>
            <p class="row-eyebrow">#{{ product.priceId }} · {{ product.productType }}</p>
            <h3>{{ product.name }}</h3>
            <p class="row-desc">{{ product.description }}</p>
          </div>
          <div class="row-actions">
            <strong>¥{{ product.price.toFixed(2) }}</strong>
            <span class="status-tag" :class="{ off: product.isActive === false }">
              {{ product.isActive === false ? '已下架' : '在售' }}
            </span>
            <button type="button" class="ghost-btn" :disabled="isPreview" @click="startEdit(product)">编辑</button>
            <button type="button" class="ghost-btn" :disabled="saving || isPreview" @click="toggleActive(product)">
              {{ product.isActive === false ? '上架' : '下架' }}
            </button>
          </div>
        </article>
      </section>
    </template>
  </div>
</template>

<style scoped>
.manage-page {
  max-width: 980px;
}

.preview-banner {
  margin: 0 0 16px;
  padding: 12px 16px;
  border-radius: 12px;
  background: #fff7ed;
  color: #c2410c;
}

.notice-banner {
  margin: 0 0 16px;
  padding: 12px 16px;
  border-radius: 12px;
  background: #e8f7ee;
  color: #15803d;
}

.inline-error {
  margin: 0 0 16px;
  color: var(--tj-danger);
}

.panel {
  margin-bottom: 20px;
  padding: 24px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.panel h2 {
  margin: 0 0 16px;
  font-size: 20px;
  color: #142239;
}

.form-grid {
  display: grid;
  gap: 12px;
}

.form-grid label {
  display: grid;
  gap: 6px;
  color: #2a3c59;
  font-size: 14px;
}

.form-grid input[type='text'],
.form-grid input[type='number'] {
  padding: 10px 12px;
  border: 1px solid #d7e0ef;
  border-radius: 10px;
}

.checkbox-row {
  display: flex !important;
  align-items: center;
  gap: 8px;
}

.btn-row {
  display: flex;
  gap: 10px;
}

.hint,
.row-desc,
.empty-text {
  margin: 12px 0 0;
  color: var(--tj-text-muted);
  line-height: 1.6;
}

.product-row {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding: 16px 0;
  border-top: 1px solid #eef2f7;
}

.row-eyebrow {
  margin: 0;
  color: #72819a;
  font-size: 12px;
}

.product-row h3 {
  margin: 6px 0 0;
  color: #142239;
}

.row-actions {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 8px;
  min-width: 140px;
}

.status-tag {
  padding: 4px 10px;
  border-radius: 999px;
  background: #e8f7ee;
  color: #15803d;
  font-size: 12px;
}

.status-tag.off {
  background: #f3f4f6;
  color: #6b7280;
}

.primary-btn,
.ghost-btn {
  padding: 8px 14px;
  border-radius: 999px;
  font-weight: 600;
  cursor: pointer;
}

.primary-btn {
  border: none;
  background: #4d77ff;
  color: #fff;
}

.ghost-btn {
  border: 1px solid #d7e0ef;
  background: #fff;
  color: #2a3c59;
}

.primary-btn:disabled,
.ghost-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}
</style>
