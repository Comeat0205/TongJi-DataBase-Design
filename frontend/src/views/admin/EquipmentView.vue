<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ApiError } from '@/api/http'
import { createEquipment, deleteEquipment, getEquipmentManagementList, updateEquipment, uploadEquipmentImage, type EquipmentItem } from '@/api/equipment'
import { getVenueManagementList, type VenueItem } from '@/api/venues'
import StateCard from '@/components/ui/StateCard.vue'

const loading = ref(true)
const submitting = ref(false)
const imageUploading = ref(false)
const errorMessage = ref('')
const equipmentList = ref<EquipmentItem[]>([])
const venues = ref<VenueItem[]>([])
const dialogOpen = ref(false)
const editingEquipmentId = ref<number | null>(null)
const previewImageUrl = ref('')
const fileInputRef = ref<HTMLInputElement | null>(null)
const venuePickerOpen = ref(false)
const venuePickerMode = ref<'form' | 'filter'>('form')
const venueKeyword = ref('')

const filters = reactive({
  keyword: '',
  status: 'active' as 'all' | 'active' | 'inactive',
  venueId: null as number | null,
  venueName: '',
})

const form = reactive({
  equipName: '',
  venueId: null as number | null,
  venueName: '',
  imageUrl: '',
  status: '正常' as '正常' | '停用',
})

const isEditing = computed(() => editingEquipmentId.value !== null)
const visibleEquipment = computed(() => equipmentList.value)
const filteredVenueList = computed(() => {
  const keyword = venueKeyword.value.trim()
  if (!keyword) return venues.value
  return venues.value.filter((item) => String(item.venueId).includes(keyword) || item.venueName.includes(keyword))
})

function resolveStatusLabel(value?: string) {
  return value === '停用' ? '停用' : '正常'
}

function resolveBadgeTone(value?: string) {
  return value === '停用' ? 'is-inactive' : 'is-active'
}

function resolveImageUrl(value?: string | null) {
  if (!value) return ''
  if (/^https?:\/\//i.test(value)) {
    return value
  }
  return value.startsWith('/') ? value : `/${value}`
}

function formatDate(value?: string) {
  if (!value) return '自动生成'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '自动生成'
  return `${date.getFullYear()}-${`${date.getMonth() + 1}`.padStart(2, '0')}-${`${date.getDate()}`.padStart(2, '0')}`
}

function resetForm() {
  form.equipName = ''
  form.venueId = null
  form.venueName = ''
  form.imageUrl = ''
  form.status = '1'
  previewImageUrl.value = ''
  editingEquipmentId.value = null
  if (fileInputRef.value) {
    fileInputRef.value.value = ''
  }
}

async function loadVenues() {
  venues.value = await getVenueManagementList({ status: 'all' })
}

async function loadEquipment() {
  loading.value = true
  errorMessage.value = ''
  try {
    equipmentList.value = await getEquipmentManagementList({
      keyword: filters.keyword.trim() || undefined,
      status: filters.status,
      venueId: filters.venueId || undefined,
    })
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '器材列表加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

function searchEquipment() {
  loadEquipment()
}

function resetFilters() {
  filters.keyword = ''
  filters.status = 'active'
  filters.venueId = null
  filters.venueName = ''
  loadEquipment()
}

function openCreateDialog() {
  dialogOpen.value = true
  errorMessage.value = ''
  resetForm()
}

function openEditDialog(item: EquipmentItem) {
  dialogOpen.value = true
  errorMessage.value = ''
  editingEquipmentId.value = item.equipId
  form.equipName = item.equipName
  form.venueId = item.venueId ?? ''
  form.venueName = resolveVenueName(item.venueId)
  form.imageUrl = item.imageUrl ?? ''
  form.status = item.status === '0' ? '0' : '1'
  previewImageUrl.value = resolveImageUrl(item.imageUrl)
  if (fileInputRef.value) {
    fileInputRef.value.value = ''
  }
}

function closeDialog() {
  dialogOpen.value = false
  resetForm()
}

function normalizeVenueId(value?: string | number | null) {
  if (value === undefined || value === null || value === '') return ''
  return String(value)
}

function resolveVenueName(venueId?: string | number | null) {
  const normalizedVenueId = normalizeVenueId(venueId)
  if (!normalizedVenueId) return ''
  return venues.value.find((item) => normalizeVenueId(item.venueId) === normalizedVenueId)?.venueName ?? ''
}

function openVenuePicker(mode: 'form' | 'filter') {
  venuePickerMode.value = mode
  venueKeyword.value = ''
  venuePickerOpen.value = true
}

function closeVenuePicker() {
  venuePickerOpen.value = false
  venueKeyword.value = ''
}

function selectVenue(item: VenueItem) {
  if (venuePickerMode.value === 'form') {
    form.venueId = item.venueId
    form.venueName = item.venueName
  } else {
    filters.venueId = item.venueId
    filters.venueName = item.venueName
    loadEquipment()
  }
  closeVenuePicker()
}

function selectAllVenues() {
  filters.venueId = null
  filters.venueName = ''
  loadEquipment()
  closeVenuePicker()
}

async function handleImageChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return

  imageUploading.value = true
  errorMessage.value = ''
  try {
    const result = await uploadEquipmentImage(file)
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
  const equipName = form.equipName.trim()
  if (!equipName) {
    errorMessage.value = '请输入器材名称。'
    return
  }
  if (!form.venueId) {
    errorMessage.value = '请选择所属场馆。'
    return
  }

  submitting.value = true
  errorMessage.value = ''
  try {
    if (isEditing.value && editingEquipmentId.value !== null) {
      await updateEquipment(editingEquipmentId.value, {
        equipName,
        venueId: form.venueId,
        imageUrl: form.imageUrl || undefined,
        status: form.status,
      })
    } else {
      await createEquipment({
        equipName,
        venueId: form.venueId,
        imageUrl: form.imageUrl || undefined,
      })
    }
    await loadEquipment()
    closeDialog()
  } catch (error) {
    errorMessage.value = error instanceof ApiError || error instanceof Error ? error.message : '保存失败，请稍后重试。'
  } finally {
    submitting.value = false
  }
}

async function handleDelete(item: EquipmentItem) {
  if (submitting.value) return
  const confirmed = window.confirm(`确认停用器材“${item.equipName}”吗？`)
  if (!confirmed) return

  submitting.value = true
  errorMessage.value = ''
  try {
    await deleteEquipment(item.equipId)
    await loadEquipment()
  } catch (error) {
    errorMessage.value = error instanceof ApiError || error instanceof Error ? error.message : '停用失败，请稍后重试。'
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  await loadVenues()
  await loadEquipment()
})
</script>

<template>
  <div class="admin-grid-view">
    <section class="page-head">
      <div>
        <p class="eyebrow">管理员端</p>
        <h1>器材管理</h1>
      </div>
      <div class="head-actions">
        <button type="button" class="btn-ghost" @click="resetFilters">重置</button>
        <button type="button" class="btn-primary" @click="openCreateDialog">添加</button>
      </div>
    </section>

    <section class="filter-bar filter-bar-extended">
      <input v-model="filters.keyword" class="search-input" type="text" placeholder="搜索 器材编号 / 器材名称 / 场馆编号" @keyup.enter="searchEquipment" />
      <div class="control-group">
        <select v-model="filters.status" class="select-input compact-select" @change="loadEquipment">
          <option value="all">全部</option>
          <option value="active">正常</option>
          <option value="inactive">停用</option>
        </select>
        <button type="button" class="btn-ghost venue-filter-button" @click="openVenuePicker('filter')">
          {{ filters.venueName ? `${filters.venueName}` : '选择场馆' }}
        </button>

        <button type="button" class="btn-primary" @click="searchEquipment">搜索</button>
      </div>
    </section>

    <StateCard v-if="errorMessage" :message="errorMessage" type="error" />
    <div v-else-if="loading" class="loading-state">加载中...</div>

    <section v-else class="grid-card">
      <div class="grid-head">
        <span>共 {{ visibleEquipment.length }} 条</span>
      </div>
      <div v-if="!visibleEquipment.length" class="empty-state">暂无器材数据</div>
      <div v-else class="card-grid">
        <article v-for="equipment in visibleEquipment" :key="equipment.equipId" class="equipment-card compact-card" @click="openEditDialog(equipment)">
          <div class="cover-wrap">
            <img v-if="equipment.imageUrl" :src="resolveImageUrl(equipment.imageUrl)" :alt="equipment.equipName" class="cover-image" />
            <div v-else class="cover-placeholder">
              <span>暂无图片</span>
            </div>
            <span class="status-pill" :class="resolveBadgeTone(equipment.status)">{{ resolveStatusLabel(equipment.status) }}</span>
          </div>

          <div class="card-body">
            <div class="title-row">
              <div>
                <h3>{{ equipment.equipName }}</h3>
              </div>
            </div>

            <div class="meta-grid single-meta-grid">
              <div class="meta-inline">
                <span class="meta-inline-item">
                  <span class="meta-label small-label">购置日期</span>
                  <strong class="meta-value small-value">{{ formatDate(equipment.purchaseDate) }}</strong>
                </span>
                <span class="meta-inline-item">
                  <span class="meta-label">所属场馆</span>
                  <strong class="meta-value">{{ resolveVenueName(equipment.venueId) || '未选择' }}</strong>
                </span>
              </div>
            </div>
          </div>
        </article>
      </div>
    </section>

    <div v-if="dialogOpen" class="detail-mask" @click.self="closeDialog">
      <section class="detail-popup equipment-popup">
        <div class="detail-popup-head">
          <div>
            <p class="eyebrow">{{ isEditing ? '编辑器材' : '新增器材' }}</p>
            <h2>{{ isEditing ? '修改器材信息' : '添加器材' }}</h2>
          </div>
          <button type="button" class="btn-ghost" @click="closeDialog">关闭</button>
        </div>

        <div class="equipment-form-layout">
          <div class="image-panel">
            <div class="image-preview">
              <img v-if="previewImageUrl" :src="previewImageUrl" alt="器材图片预览" class="preview-image" />
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
              <span class="detail-label">器材名称</span>
              <input v-model="form.equipName" class="search-input" type="text" placeholder="请输入器材名称" />
            </label>
            <label class="form-item">
              <span class="detail-label">所属场馆</span>
              <button type="button" class="picker-input" @click="openVenuePicker('form')">
                {{ form.venueName || '点击选择场馆' }}
              </button>
            </label>
            <label v-if="isEditing" class="form-item">
              <span class="detail-label">器材状态</span>
              <select v-model="form.status" class="select-input">
                <option value="1">正常</option>
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

    <div v-if="venuePickerOpen" class="detail-mask" @click.self="closeVenuePicker">
      <section class="detail-popup picker-popup">
        <div class="detail-popup-head">
          <div>
            <p class="eyebrow">选择场馆</p>
            <h2>场馆列表</h2>
          </div>
          <button type="button" class="btn-ghost" @click="closeVenuePicker">关闭</button>
        </div>

        <div class="picker-toolbar">
          <div class="picker-search-row">
            <input v-model="venueKeyword" class="search-input" type="text" placeholder="搜索 场馆ID / 场馆名称" />
          </div>
          <button v-if="venuePickerMode === 'filter'" type="button" class="picker-all-row" @click="selectAllVenues">
            <span class="picker-id">全部</span>
            <strong class="picker-name">查看所有场馆下的器材</strong>
          </button>
        </div>

        <div class="picker-list-shell">
          <div class="picker-list">
            <div v-if="!filteredVenueList.length" class="empty-state">暂无可选场馆</div>
            <button v-for="venue in filteredVenueList" :key="venue.venueId" type="button" class="picker-row" @click="selectVenue(venue)">
              <span class="picker-id">{{ venue.venueId }}</span>
              <strong class="picker-name">{{ venue.venueName }}</strong>
            </button>
          </div>
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
.filter-bar-extended { gap: 16px; }
.control-group, .head-actions, .dialog-actions { display: flex; gap: 10px; justify-content: flex-end; align-items: center; flex-wrap: wrap; }
.search-input, .select-input, .picker-input { width: 100%; border: 1px solid #d1d5db; border-radius: 10px; padding: 10px 12px; outline: none; background: #fff; }
.picker-input { text-align: left; cursor: pointer; }
.compact-select { width: 120px; }
.venue-filter-button { min-width: 140px; }
.card-grid { margin-top: 16px; display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 12px; align-items: start; }
.equipment-card { border: 1px solid #e5e7eb; border-radius: 16px; overflow: hidden; background: #fcfdff; display: grid; cursor: pointer; transition: box-shadow .2s ease, transform .2s ease; }
.compact-card { width: 100%; justify-self: stretch; }
.equipment-card:hover { box-shadow: 0 12px 28px rgba(15, 23, 42, 0.08); transform: translateY(-2px); }
.cover-wrap { position: relative; aspect-ratio: 16 / 9; background: #eef2ff; }
.cover-image { width: 100%; height: 100%; object-fit: cover; display: block; }
.cover-placeholder { width: 100%; height: 100%; display: grid; place-items: center; color: #94a3b8; background: linear-gradient(135deg, #eff6ff 0%, #eef2ff 100%); font-weight: 600; }
.large-placeholder { min-height: 220px; border-radius: 16px; border: 1px dashed #cbd5e1; }
.status-pill { position: absolute; top: 14px; right: 14px; display: inline-flex; align-items: center; padding: 6px 10px; border-radius: 999px; font-size: 13px; font-weight: 600; }
.is-active { color: #1d4ed8; background: rgba(219, 234, 254, 0.96); }
.is-inactive { color: #991b1b; background: rgba(254, 226, 226, 0.96); }
.card-body { display: grid; gap: 10px; padding: 14px; }
.title-row h3 { margin: 0; font-size: 18px; color: #111827; }
.meta-grid { display: grid; gap: 10px; }
.single-meta-grid { grid-template-columns: 1fr; }
.meta-inline { display: flex; justify-content: space-between; gap: 12px; align-items: flex-start; }
.meta-inline-item { display: grid; gap: 4px; min-width: 0; flex: 1; }
.meta-value { display: block; margin-top: 0; font-size: 14px; color: #111827; }
.small-label { font-size: 12px; }
.small-value { font-size: 13px; }
.loading-state, .empty-state { padding: 32px 0; text-align: center; color: #6b7280; }
.detail-mask { position: fixed; inset: 0; background: rgba(15, 23, 42, 0.28); display: grid; place-items: center; padding: 20px; z-index: 40; }
.detail-popup { width: min(760px, 100%); background: #fff; border-radius: 18px; border: 1px solid #e5e7eb; padding: 22px; }
.equipment-popup { width: min(820px, 100%); }
.picker-popup { width: min(720px, 100%); }
.detail-popup-head { display: flex; justify-content: space-between; gap: 16px; align-items: flex-start; margin-bottom: 18px; }
.detail-popup-head h2 { margin: 4px 0 0; }
.equipment-form-layout { display: grid; grid-template-columns: minmax(240px, 300px) minmax(0, 1fr); gap: 20px; align-items: start; }
.image-panel, .form-grid { display: grid; gap: 14px; }
.image-preview { min-height: 180px; }
.preview-image { width: 100%; min-height: 180px; max-height: 240px; object-fit: cover; border-radius: 16px; border: 1px solid #e5e7eb; display: block; }
.upload-button { display: inline-flex; justify-content: center; align-items: center; min-height: 42px; border-radius: 10px; border: 1px dashed #94a3b8; color: #334155; background: #f8fafc; cursor: pointer; }
.upload-button.disabled { opacity: .6; cursor: not-allowed; }
.form-item { display: grid; gap: 8px; }
.detail-label { color: #6b7280; font-size: 13px; }
.picker-toolbar { display: grid; gap: 12px; margin-bottom: 14px; }
.picker-search-row { margin-bottom: 0; }
.picker-list-shell { border: 1px solid #e5e7eb; border-radius: 16px; background: linear-gradient(180deg, #fbfdff 0%, #f8fafc 100%); padding: 12px; }
.picker-list { display: grid; gap: 10px; max-height: min(56vh, 440px); overflow-y: auto; padding-right: 4px; }
.picker-all-row, .picker-row { display: grid; grid-template-columns: 120px minmax(0, 1fr); align-items: center; gap: 12px; width: 100%; text-align: left; border: 1px solid #e5e7eb; border-radius: 14px; background: #fff; padding: 14px 16px; cursor: pointer; transition: border-color .2s ease, box-shadow .2s ease, transform .2s ease, background .2s ease; }
.picker-all-row { background: #f8fbff; border-color: #dbeafe; }
.picker-all-row:hover, .picker-row:hover { border-color: #bfdbfe; background: #f8fbff; box-shadow: 0 10px 24px rgba(15, 23, 42, 0.06); transform: translateY(-1px); }
.picker-id { color: #475569; font-variant-numeric: tabular-nums; }
.picker-name { color: #111827; }
.btn-primary, .btn-ghost { border-radius: 10px; padding: 9px 14px; border: 1px solid transparent; cursor: pointer; }
.btn-primary { background: #2563eb; color: #fff; }
.btn-ghost { background: #fff; border-color: #d1d5db; color: #1f2937; }
@media (max-width: 1180px) {
  .card-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
@media (max-width: 820px) {
  .filter-bar, .equipment-form-layout { grid-template-columns: 1fr; }
  .card-grid { grid-template-columns: 1fr; }
  .picker-row { grid-template-columns: 1fr; }
}
</style>
