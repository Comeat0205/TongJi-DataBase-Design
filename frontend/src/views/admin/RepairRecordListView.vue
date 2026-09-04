<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ApiError } from '@/api/http'
import {
  createRepairRecord,
  getRepairRecordOptions,
  getRepairRecords,
  updateRepairRecordStatus,
  type RepairRecord,
  type RepairRecordOptions,
  type RepairStatus,
} from '@/api/repair-records'
import PageHeader from '@/components/ui/PageHeader.vue'
import SearchableEntitySelect from '@/components/ui/SearchableEntitySelect.vue'
import StateCard from '@/components/ui/StateCard.vue'

const route = useRoute()
const isPreview = computed(() => route.path.startsWith('/preview/admin'))
const records = ref<RepairRecord[]>([])
const loading = ref(true)
const saving = ref(false)
const errorMessage = ref('')
const noticeMessage = ref('')
const selectedStatus = ref<RepairStatus | ''>('')
const options = ref<RepairRecordOptions>({ equipment: [], employees: [] })
const optionsLoading = ref(true)
const optionsErrorMessage = ref('')

const createForm = reactive({
  equipId: undefined as number | undefined,
  description: '',
})

const progressForm = reactive({
  recordId: 0,
  empId: undefined as number | undefined,
  repairCost: undefined as number | undefined,
})

function formatTime(value?: string) {
  return value ? new Date(value).toLocaleString('zh-CN') : '—'
}

function statusClass(status: RepairStatus) {
  return {
    waiting: status === '待处理',
    working: status === '维修中',
    done: status === '已完成',
  }
}

function nextStatus(status: RepairStatus): RepairStatus | undefined {
  if (status === '待处理') return '维修中'
  if (status === '维修中') return '已完成'
  return undefined
}

async function loadRecords() {
  loading.value = true
  errorMessage.value = ''

  try {
    records.value = await getRepairRecords(selectedStatus.value || undefined)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '报修记录加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

async function loadOptions() {
  optionsLoading.value = true
  optionsErrorMessage.value = ''

  try {
    options.value = await getRepairRecordOptions()
  } catch (error) {
    optionsErrorMessage.value =
      error instanceof ApiError ? error.message : '器材和员工选项加载失败，请稍后重试。'
  } finally {
    optionsLoading.value = false
  }
}

async function handleCreate() {
  if (isPreview.value) {
    errorMessage.value = '预览模式只展示页面，请登录员工账号后再新建报修。'
    return
  }

  if (!createForm.equipId || !createForm.description.trim()) {
    errorMessage.value = '请填写器材编号和问题描述。'
    return
  }

  saving.value = true
  errorMessage.value = ''
  noticeMessage.value = ''

  try {
    await createRepairRecord({
      equipId: createForm.equipId,
      description: createForm.description.trim(),
    })
    noticeMessage.value = '报修记录已创建。'
    createForm.equipId = undefined
    createForm.description = ''
    await loadRecords()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '新建报修失败，请稍后重试。'
  } finally {
    saving.value = false
  }
}

function startProgress(record: RepairRecord) {
  progressForm.recordId = record.recordId
  progressForm.empId = record.empId
  progressForm.repairCost = record.repairCost
  noticeMessage.value = ''
  errorMessage.value = ''
}

function cancelProgress() {
  progressForm.recordId = 0
}

async function advanceStatus(record: RepairRecord) {
  if (isPreview.value) {
    errorMessage.value = '预览模式只展示页面，请登录员工账号后再更新状态。'
    return
  }

  const targetStatus = nextStatus(record.status)
  if (!targetStatus) return

  if (!progressForm.empId) {
    errorMessage.value = '进入维修流程前需要填写维修员工编号。'
    return
  }

  saving.value = true
  errorMessage.value = ''
  noticeMessage.value = ''

  try {
    await updateRepairRecordStatus(record.recordId, {
      status: targetStatus,
      empId: progressForm.empId,
      repairCost: progressForm.repairCost,
    })
    noticeMessage.value = `报修 #${record.recordId} 已更新为“${targetStatus}”。`
    progressForm.recordId = 0
    await loadRecords()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '状态更新失败，请稍后重试。'
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  await Promise.all([loadRecords(), loadOptions()])
})
</script>

<template>
  <div class="repair-page">
    <PageHeader title="器材报修" subtitle="登记器材故障，跟进维修负责人、费用和处理进度。" />

    <p v-if="isPreview" class="preview-banner">预览模式：可以查看页面和筛选记录，但不能新建或更新报修。</p>
    <p v-if="noticeMessage" class="notice-banner">{{ noticeMessage }}</p>
    <p v-if="errorMessage && records.length > 0" class="inline-error">{{ errorMessage }}</p>
    <p v-if="optionsErrorMessage" class="inline-error">{{ optionsErrorMessage }}</p>

    <section class="panel">
      <h2>新建报修</h2>
      <form class="create-form" @submit.prevent="handleCreate">
        <SearchableEntitySelect
          v-model="createForm.equipId"
          class="equipment-field"
          label="器材"
          :options="options.equipment"
          :disabled="optionsLoading"
          :placeholder="optionsLoading ? '器材加载中...' : '输入器材名称或编号'"
          empty-text="暂无可选器材"
          required
        />
        <label class="description-field">
          问题描述
          <input v-model="createForm.description" type="text" maxlength="200" placeholder="简单说明器材故障情况" />
        </label>
        <button type="submit" class="primary-btn" :disabled="saving || isPreview">提交报修</button>
      </form>
    </section>

    <section class="panel">
      <div class="panel-heading">
        <h2>报修记录</h2>
        <label class="filter-field">
          状态
          <select v-model="selectedStatus" @change="loadRecords">
            <option value="">全部</option>
            <option value="待处理">待处理</option>
            <option value="维修中">维修中</option>
            <option value="已完成">已完成</option>
          </select>
        </label>
      </div>

      <StateCard v-if="loading" message="报修记录加载中..." />
      <StateCard v-else-if="errorMessage && records.length === 0" :message="errorMessage" type="error" />
      <p v-else-if="records.length === 0" class="empty-text">当前没有符合条件的报修记录。</p>

      <div v-else class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>记录</th>
              <th>器材</th>
              <th>问题</th>
              <th>维修人员</th>
              <th>费用</th>
              <th>状态</th>
              <th>上报时间</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <template v-for="record in records" :key="record.recordId">
              <tr>
                <td>#{{ record.recordId }}</td>
                <td>{{ record.equipName }}（{{ record.equipId }}）</td>
                <td class="description-cell">{{ record.description || '—' }}</td>
                <td>{{ record.employeeName || record.empId || '未分配' }}</td>
                <td>¥{{ record.repairCost.toFixed(2) }}</td>
                <td><span class="status-tag" :class="statusClass(record.status)">{{ record.status }}</span></td>
                <td>{{ formatTime(record.reportTime) }}</td>
                <td>
                  <button
                    v-if="nextStatus(record.status)"
                    type="button"
                    class="ghost-btn"
                    :disabled="saving || isPreview"
                    @click="startProgress(record)"
                  >
                    推进处理
                  </button>
                  <span v-else class="finished-text">已办结</span>
                </td>
              </tr>
              <tr v-if="progressForm.recordId === record.recordId" class="progress-row">
                <td colspan="8">
                  <form class="progress-form" @submit.prevent="advanceStatus(record)">
                    <strong>更新为“{{ nextStatus(record.status) }}”</strong>
                    <SearchableEntitySelect
                      v-model="progressForm.empId"
                      label="维修员工"
                      :options="options.employees"
                      :disabled="optionsLoading"
                      :placeholder="optionsLoading ? '员工加载中...' : '输入员工姓名或编号'"
                      empty-text="暂无可选员工"
                      required
                    />
                    <label>
                      维修费用
                      <input v-model.number="progressForm.repairCost" type="number" min="0" step="0.01" />
                    </label>
                    <button type="button" class="ghost-btn" @click="cancelProgress">取消</button>
                    <button type="submit" class="primary-btn" :disabled="saving">确认更新</button>
                  </form>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<style scoped>
.repair-page { display: grid; gap: 20px; }
.panel { padding: 24px; border-radius: var(--tj-radius); background: var(--tj-card-bg); box-shadow: var(--tj-shadow); }
.panel h2 { margin: 0; font-size: 20px; color: var(--tj-text); }
.preview-banner, .notice-banner { margin: 0; padding: 12px 16px; border-radius: 12px; }
.preview-banner { background: #fff7ed; color: #c2410c; }
.notice-banner { background: #e8f7ee; color: #15803d; }
.inline-error { margin: 0; color: var(--tj-danger); }
.create-form, .progress-form { display: flex; gap: 12px; align-items: end; flex-wrap: wrap; }
.create-form { margin-top: 18px; }
.create-form label, .progress-form label, .filter-field { display: grid; gap: 6px; color: #2a3c59; font-size: 14px; }
.description-field { flex: 1; min-width: 280px; }
.equipment-field { min-width: 240px; }
input, select { min-height: 40px; padding: 8px 11px; border: 1px solid #d7e0ef; border-radius: 9px; background: #fff; color: var(--tj-text); }
.panel-heading { display: flex; justify-content: space-between; gap: 16px; align-items: end; margin-bottom: 18px; }
.table-wrap { overflow-x: auto; }
table { width: 100%; border-collapse: collapse; }
th, td { padding: 12px 10px; border-bottom: 1px solid #edf1f7; text-align: left; vertical-align: middle; font-size: 14px; }
th { color: var(--tj-text-muted); font-size: 13px; white-space: nowrap; }
.description-cell { min-width: 180px; max-width: 320px; }
.status-tag { display: inline-block; padding: 4px 10px; border-radius: 999px; font-size: 12px; white-space: nowrap; }
.status-tag.waiting { background: #fff7ed; color: #c2410c; }
.status-tag.working { background: #e8f0ff; color: #285cff; }
.status-tag.done { background: #e8f7ee; color: #15803d; }
.progress-row td { background: #f7f9fd; }
.progress-form { justify-content: flex-end; }
.primary-btn, .ghost-btn { min-height: 38px; padding: 8px 14px; border-radius: 9px; font-weight: 600; cursor: pointer; }
.primary-btn { border: 0; background: var(--tj-primary); color: #fff; }
.ghost-btn { border: 1px solid #d7e0ef; background: #fff; color: #2a3c59; }
.primary-btn:disabled, .ghost-btn:disabled { opacity: .6; cursor: not-allowed; }
.finished-text, .empty-text { color: var(--tj-text-muted); }
.empty-text { margin: 24px 0 0; text-align: center; }
@media (max-width: 760px) { .panel-heading { align-items: stretch; flex-direction: column; } .create-form { align-items: stretch; flex-direction: column; } }
</style>
