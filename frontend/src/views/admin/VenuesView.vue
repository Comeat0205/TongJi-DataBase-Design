<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ApiError } from '@/api/http'
import {
  createVenue,
  deleteVenue,
  getVenueManagementList,
  updateVenue,
  uploadVenueImage,
  type VenueItem,
} from '@/api/venues'
import StateCard from '@/components/ui/StateCard.vue'

const loading = ref(true)
const submitting = ref(false)
const imageUploading = ref(false)
const errorMessage = ref('')
const venues = ref<VenueItem[]>([])
const dialogOpen = ref(false)
const editingVenueId = ref<number | null>(null)
const previewImageUrl = ref('')
const fileInputRef = ref<HTMLInputElement | null>(null)

const filters = reactive({
  keyword: '',
  status: 'active' as 'all' | 'active' | 'inactive',
})

const form = reactive({
  venueName: '',
  maxCapacity: '',
  venueStatus: '1' as '1' | '0',
  imageUrl: '',
})

const isEditing = computed(() => editingVenueId.value !== null)
const visibleVenues = computed(() => venues.value)

function resolveStatusLabel(value?: string) {
  return value === '0' ? '停用' : '启用'
}

function resolveBadgeTone(value?: string) {
  return value === '0' ? 'is-inactive' : 'is-active'
}

function resolveImageUrl(value?: string | null) {
  if (!value) return ''
  if (/^https?:\/\//i.test(value)) {
    return value
  }
  return value.startsWith('/') ? value : `/${value}`
}

function resolveCapacityPercent(item: VenueItem) {
  if (!item.maxCapacity || item.maxCapacity <= 0) return 0
  const current = item.currentCapacity ?? 0
  return Math.min(100, Math.max(0, Math.round((current / item.maxCapacity) * 100)))
}

function resetForm() {
  form.venueName = ''
  form.maxCapacity = ''
  form.venueStatus = '1'
  form.imageUrl = ''
  previewImageUrl.value = ''
  editingVenueId.value = null
  if (fileInputRef.value) {
    fileInputRef.value.value = ''
  }
}

async function loadVenues() {
  loading.value = true
  errorMessage.value = ''
  try {
    venues.value = await getVenueManagementList({
      keyword: filters.keyword.trim() || undefined,
      status: filters.status,
    })
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '场馆列表加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

function searchVenues() {
  loadVenues()
}

function resetFilters() {
  filters.keyword = ''
  filters.status = 'active'
  loadVenues()
}

function openCreateDialog() {
  dialogOpen.value = true
  errorMessage.value = ''
  resetForm()
}

function openEditDialog(item: VenueItem) {
  dialogOpen.value = true
  errorMessage.value = ''
  editingVenueId.value = item.venueId
  form.venueName = item.venueName
  form.maxCapacity = String(item.maxCapacity)
  form.venueStatus = item.venueStatus === '0' ? '0' : '1'
  form.imageUrl = item.imageUrl ?? ''
  previewImageUrl.value = resolveImageUrl(item.imageUrl)
  if (fileInputRef.value) {
    fileInputRef.value.value = ''
  }
}

function closeDialog() {
  dialogOpen.value = false
  resetForm()
}

async function handleImageChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) {
    return
  }

  imageUploading.value = true
  errorMessage.value = ''
  try {
    const result = await uploadVenueImage(file)
    form.imageUrl = result.imageUrl
    previewImageUrl.value = resolveImageUrl(result.imageUrl)
  } catch (error) {
    errorMessage.value = error instanceof ApiError || error instanceof Error ? error.message : '图片上传失败，请稍后重试。'
    if (fileInputRef.value) {
      fileInputRef.value.value = ''
    }
  } finally {
    imageUploading.value = false
  }
}

async function handleSubmit() {
  const venueName = form.venueName.trim()
  const maxCapacity = Number(form.maxCapacity)

  if (!venueName) {
    errorMessage.value = '请输入场馆名称。'
    return
  }
  if (!Number.isFinite(maxCapacity) || maxCapacity <= 0) {
    errorMessage.value = '请输入合法的最大容量。'
    return
  }

  submitting.value = true
  errorMessage.value = ''
  try {
    if (isEditing.value && editingVenueId.value !== null) {
      await updateVenue(editingVenueId.value, {
        venueName,
        maxCapacity,
        venueStatus: form.venueStatus,
        imageUrl: form.imageUrl || undefined,
      })
    } else {
      await createVenue({
        venueName,
        maxCapacity,
        imageUrl: form.imageUrl || undefined,
      })
    }

    await loadVenues()
    closeDialog()
  } catch (error) {
    errorMessage.value = error instanceof ApiError || error instanceof Error ? error.message : '保存失败，请稍后重试。'
  } finally {
    submitting.value = false
  }
}

async function handleDelete(item: VenueItem) {
  if (submitting.value) return
  const confirmed = window.confirm(`确认停用场馆“${item.venueName}”吗？`)
  if (!confirmed) return

  submitting.value = true
  errorMessage.value = ''
  try {
    await deleteVenue(item.venueId)
    await loadVenues()
  } catch (error) {
    errorMessage.value = error instanceof ApiError || error instanceof Error ? error.message : '停用失败，请稍后重试。'
  } finally {
    submitting.value = false
  }
}

onMounted(loadVenues)
</script>

<template>
  <div class="admin-grid-view">
    <section class="page-head">
      <div>
        <p class="eyebrow">管理员端</p>
        <h1>场馆管理</h1>
      </div>
      <div class="head-actions">
        <button type="button" class="btn-ghost" @click="resetFilters">重置</button>
        <button type="button" class="btn-primary" @click="openCreateDialog">添加</button>
      </div>
    </section>

    <section class="filter-bar">
      <input v-model="filters.keyword" class="search-input" type="text" placeholder="搜索 场馆编号 / 场馆名称" @keyup.enter="searchVenues" />
      <div class="control-group">
        <select v-model="filters.status" class="select-input compact-select" @change="loadVenues">
          <option value="all">全部</option>
          <option value="active">启用</option>
          <option value="inactive">停用</option>
        </select>
        <button type="button" class="btn-primary" @click="searchVenues">搜索</button>
      </div>
    </section>

    <StateCard v-if="errorMessage" :message="errorMessage" type="error" />
    <div v-else-if="loading" class="loading-state">加载中...</div>

    <section v-else class="grid-card">
      <div class="grid-head">
        <span>共 {{ visibleVenues.length }} 条</span>
      </div>

      <div v-if="!visibleVenues.length" class="empty-state">暂无场馆数据</div>

      <div v-else class="card-grid">
        <article v-for="venue in visibleVenues" :key="venue.venueId" class="venue-card" @click="openEditDialog(venue)">
          <div class="cover-wrap">
            <img v-if="venue.imageUrl" :src="resolveImageUrl(venue.imageUrl)" :alt="venue.venueName" class="cover-image" />
            <div v-else class="cover-placeholder">
              <span>暂无图片</span>
            </div>
            <span class="status-pill" :class="resolveBadgeTone(venue.venueStatus)">{{ resolveStatusLabel(venue.venueStatus) }}</span>
          </div>

          <div class="card-body">
            <div class="title-row">
              <div>
                <h3>{{ venue.venueName }}</h3>
              </div>
            </div>

            <div class="capacity-box">
              <div class="capacity-head">
                <span class="meta-label">容量占用</span>
                <strong>{{ venue.currentCapacity ?? 0 }} / {{ venue.maxCapacity }}</strong>
              </div>
              <div class="capacity-track">
                <div class="capacity-fill" :style="{ width: `${resolveCapacityPercent(venue)}%` }"></div>
              </div>
            </div>


          </div>
        </article>
      </div>
    </section>

    <div v-if="dialogOpen" class="detail-mask" @click.self="closeDialog">
      <section class="detail-popup venue-popup">
        <div class="detail-popup-head">
          <div>
            <p class="eyebrow">{{ isEditing ? '编辑场馆' : '新增场馆' }}</p>
            <h2>{{ isEditing ? '修改场馆信息' : '添加场馆' }}</h2>
          </div>
          <button type="button" class="btn-ghost" @click="closeDialog">关闭</button>
        </div>

        <div class="venue-form-layout">
          <div class="image-panel">
            <div class="image-preview">
              <img v-if="previewImageUrl" :src="previewImageUrl" alt="场馆图片预览" class="preview-image" />
              <div v-else class="cover-placeholder large-placeholder">
                <span>上传后预览</span>
              </div>
            </div>
            <label class="upload-button" :class="{ disabled: imageUploading || submitting }">
              <input ref="fileInputRef" type="file" accept="image/png,image/jpeg,image/jpg,image/webp" hidden :disabled="imageUploading || submitting" @change="handleImageChange" />
              {{ imageUploading ? '上传中...' : '上传图片' }}
            </label>
          </div>

          <div class="form-grid single-column">
            <label class="form-item">
              <span class="detail-label">场馆名称</span>
              <input v-model="form.venueName" class="search-input" type="text" placeholder="请输入场馆名称" />
            </label>
            <label class="form-item">
              <span class="detail-label">最大容量</span>
              <input v-model="form.maxCapacity" class="search-input" type="number" min="1" placeholder="请输入最大容量" />
            </label>
            <label v-if="isEditing" class="form-item">
              <span class="detail-label">是否启用</span>
              <select v-model="form.venueStatus" class="select-input">
                <option value="1">启用</option>
                <option value="0">停用</option>
              </select>
            </label>
          </div>
        </div>

        <div class="dialog-actions">
          <button type="button" class="btn-ghost" @click="closeDialog">取消</button>
          <button type="button" class="btn-primary" :disabled="submitting || imageUploading" @click="handleSubmit">
            {{ submitting ? '保存中...' : isEditing ? '保存修改' : '确认添加' }}
          </button>
        </div>
      </section>
    </div>
  </div>
</template>

<style scoped>
.admin-grid-view { display: grid; gap: 18px; }
.page-head, .filter-bar, .grid-card { background: #fff; border: 1px solid #e5e7eb; border-radius: 14px; padding: 18px; }
.page-head { display: flex; justify-content: space-between; align-items: center; gap: 16px; }
.page-head h1 { margin: 4px 0 0; font-size: 28px; }
.eyebrow, .meta-label, .card-id { color: #6b7280; margin: 0; }
.filter-bar { display: grid; grid-template-columns: minmax(0,1fr) auto; gap: 12px; align-items: center; }
.control-group, .head-actions, .card-actions, .dialog-actions { display: flex; gap: 10px; justify-content: flex-end; }
.search-input, .select-input { width: 100%; border: 1px solid #d1d5db; border-radius: 10px; padding: 10px 12px; outline: none; }
.compact-select { width: 120px; }
.card-grid { margin-top: 16px; display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 18px; }
.venue-card { border: 1px solid #e5e7eb; border-radius: 18px; overflow: hidden; background: #fcfdff; display: grid; cursor: pointer; transition: box-shadow .2s ease, transform .2s ease; }
.venue-card:hover { box-shadow: 0 12px 28px rgba(15, 23, 42, 0.08); transform: translateY(-2px); }
.cover-wrap { position: relative; aspect-ratio: 16 / 9; background: #eef2ff; }
.cover-image { width: 100%; height: 100%; object-fit: cover; display: block; }
.cover-placeholder { width: 100%; height: 100%; display: grid; place-items: center; color: #94a3b8; background: linear-gradient(135deg, #eff6ff 0%, #eef2ff 100%); font-weight: 600; }
.large-placeholder { min-height: 220px; border-radius: 16px; border: 1px dashed #cbd5e1; }
.status-pill { position: absolute; top: 14px; right: 14px; display: inline-flex; align-items: center; padding: 6px 10px; border-radius: 999px; font-size: 13px; font-weight: 600; }
.is-active { color: #1d4ed8; background: rgba(219, 234, 254, 0.96); }
.is-inactive { color: #991b1b; background: rgba(254, 226, 226, 0.96); }
.card-body { display: grid; gap: 14px; padding: 18px; }
.title-row h3 { margin: 0 0 6px; font-size: 20px; color: #111827; }
.capacity-box { display: grid; gap: 8px; }
.capacity-head { display: flex; justify-content: space-between; gap: 12px; align-items: center; color: #374151; font-size: 14px; }
.capacity-track { height: 10px; border-radius: 999px; background: #e5e7eb; overflow: hidden; }
.capacity-fill { height: 100%; border-radius: inherit; background: linear-gradient(90deg, #60a5fa 0%, #2563eb 100%); }
.loading-state, .empty-state { padding: 32px 0; text-align: center; color: #6b7280; }
.detail-mask { position: fixed; inset: 0; background: rgba(15, 23, 42, 0.28); display: grid; place-items: center; padding: 20px; }
.detail-popup { width: min(760px, 100%); background: #fff; border-radius: 18px; border: 1px solid #e5e7eb; padding: 22px; }
.venue-popup { width: min(820px, 100%); }
.detail-popup-head { display: flex; justify-content: space-between; gap: 16px; align-items: flex-start; margin-bottom: 18px; }
.detail-popup-head h2 { margin: 4px 0 0; }
.venue-form-layout { display: grid; grid-template-columns: minmax(240px, 300px) minmax(0, 1fr); gap: 20px; align-items: start; }
.image-panel, .form-grid { display: grid; gap: 14px; }
.image-preview { min-height: 220px; }
.preview-image { width: 100%; min-height: 220px; max-height: 260px; object-fit: cover; border-radius: 16px; border: 1px solid #e5e7eb; display: block; }
.upload-button { display: inline-flex; justify-content: center; align-items: center; min-height: 42px; border-radius: 10px; border: 1px dashed #94a3b8; color: #334155; background: #f8fafc; cursor: pointer; }
.upload-button.disabled { opacity: .6; cursor: not-allowed; }
.form-item { display: grid; gap: 8px; }
.detail-label { color: #6b7280; font-size: 13px; }
.btn-primary, .btn-ghost, .btn-danger { border-radius: 10px; padding: 9px 14px; border: 1px solid transparent; cursor: pointer; }
.btn-primary { background: #2563eb; color: #fff; }
.btn-ghost { background: #fff; border-color: #d1d5db; color: #1f2937; }
.btn-danger { background: #fff1f2; border-color: #fecdd3; color: #be123c; }
@media (max-width: 1180px) {
  .card-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
@media (max-width: 820px) {
  .filter-bar, .venue-form-layout { grid-template-columns: 1fr; }
  .card-grid { grid-template-columns: 1fr; }
}
</style>
