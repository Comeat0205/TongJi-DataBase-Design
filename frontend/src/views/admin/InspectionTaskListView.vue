<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ApiError } from '@/api/http'
import {
  createInspectionTask,
  getInspectionTasks,
  updateInspectionTaskStatus,
  type InspectionStatus,
  type InspectionTask,
} from '@/api/inspection-tasks'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'

const route = useRoute()
const isPreview = computed(() => route.path.startsWith('/preview/admin'))
const tasks = ref<InspectionTask[]>([])
const loading = ref(true)
const saving = ref(false)
const errorMessage = ref('')
const noticeMessage = ref('')
const selectedStatus = ref<InspectionStatus | ''>('')

const createForm = reactive({
  venueId: undefined as number | undefined,
  empId: undefined as number | undefined,
  taskTime: '',
  remark: '',
})

const progressForm = reactive({
  taskId: 0,
  remark: '',
})

function formatTime(value: string) {
  return new Date(value).toLocaleString('zh-CN')
}

function statusClass(status: InspectionStatus) {
  return {
    waiting: status === '待执行',
    working: status === '进行中',
    done: status === '已完成',
  }
}

function nextStatus(status: InspectionStatus): InspectionStatus | undefined {
  if (status === '待执行') return '进行中'
  if (status === '进行中') return '已完成'
  return undefined
}

async function loadTasks() {
  loading.value = true
  errorMessage.value = ''

  try {
    tasks.value = await getInspectionTasks(selectedStatus.value || undefined)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '巡检任务加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

async function handleCreate() {
  if (isPreview.value) {
    errorMessage.value = '预览模式只展示页面，请登录员工账号后再创建巡检任务。'
    return
  }

  if (!createForm.venueId || !createForm.empId || !createForm.taskTime) {
    errorMessage.value = '请填写场馆、执行员工和巡检时间。'
    return
  }

  saving.value = true
  errorMessage.value = ''
  noticeMessage.value = ''

  try {
    await createInspectionTask({
      venueId: createForm.venueId,
      empId: createForm.empId,
      taskTime: createForm.taskTime,
      remark: createForm.remark.trim() || undefined,
    })
    noticeMessage.value = '巡检任务已创建。'
    createForm.venueId = undefined
    createForm.empId = undefined
    createForm.taskTime = ''
    createForm.remark = ''
    await loadTasks()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '创建巡检任务失败，请稍后重试。'
  } finally {
    saving.value = false
  }
}

function startProgress(task: InspectionTask) {
  progressForm.taskId = task.taskId
  progressForm.remark = task.remark ?? ''
  noticeMessage.value = ''
  errorMessage.value = ''
}

function cancelProgress() {
  progressForm.taskId = 0
}

async function advanceStatus(task: InspectionTask) {
  if (isPreview.value) {
    errorMessage.value = '预览模式只展示页面，请登录员工账号后再更新巡检状态。'
    return
  }

  const targetStatus = nextStatus(task.status)
  if (!targetStatus) return

  saving.value = true
  errorMessage.value = ''
  noticeMessage.value = ''

  try {
    await updateInspectionTaskStatus(task.taskId, {
      status: targetStatus,
      remark: progressForm.remark.trim() || undefined,
    })
    noticeMessage.value = `巡检任务 #${task.taskId} 已更新为“${targetStatus}”。`
    progressForm.taskId = 0
    await loadTasks()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '巡检状态更新失败，请稍后重试。'
  } finally {
    saving.value = false
  }
}

onMounted(loadTasks)
</script>

<template>
  <div class="inspection-page">
    <PageHeader title="巡检任务" subtitle="安排场馆巡检，跟进执行人员、计划时间和完成情况。" />

    <p v-if="isPreview" class="preview-banner">预览模式：可以查看页面和筛选任务，但不能创建任务或更新状态。</p>
    <p v-if="noticeMessage" class="notice-banner">{{ noticeMessage }}</p>
    <p v-if="errorMessage && tasks.length > 0" class="inline-error">{{ errorMessage }}</p>

    <section class="panel">
      <h2>安排巡检</h2>
      <form class="create-form" @submit.prevent="handleCreate">
        <label>
          场馆编号
          <input v-model.number="createForm.venueId" type="number" min="1" placeholder="例如 1" />
        </label>
        <label>
          执行员工编号
          <input v-model.number="createForm.empId" type="number" min="1" placeholder="例如 9" />
        </label>
        <label>
          计划时间
          <input v-model="createForm.taskTime" type="datetime-local" />
        </label>
        <label class="remark-field">
          备注
          <input v-model="createForm.remark" type="text" maxlength="200" placeholder="可填写巡检重点" />
        </label>
        <button type="submit" class="primary-btn" :disabled="saving || isPreview">创建任务</button>
      </form>
    </section>

    <section class="panel">
      <div class="panel-heading">
        <h2>任务列表</h2>
        <label class="filter-field">
          状态
          <select v-model="selectedStatus" @change="loadTasks">
            <option value="">全部</option>
            <option value="待执行">待执行</option>
            <option value="进行中">进行中</option>
            <option value="已完成">已完成</option>
          </select>
        </label>
      </div>

      <StateCard v-if="loading" message="巡检任务加载中..." />
      <StateCard v-else-if="errorMessage && tasks.length === 0" :message="errorMessage" type="error" />
      <p v-else-if="tasks.length === 0" class="empty-text">当前没有符合条件的巡检任务。</p>

      <div v-else class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>任务</th>
              <th>场馆</th>
              <th>执行员工</th>
              <th>计划时间</th>
              <th>备注</th>
              <th>状态</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            <template v-for="task in tasks" :key="task.taskId">
              <tr>
                <td>#{{ task.taskId }}</td>
                <td>{{ task.venueName }}（{{ task.venueId }}）</td>
                <td>{{ task.employeeName }}（{{ task.empId }}）</td>
                <td>{{ formatTime(task.taskTime) }}</td>
                <td class="remark-cell">{{ task.remark || '—' }}</td>
                <td><span class="status-tag" :class="statusClass(task.status)">{{ task.status }}</span></td>
                <td>
                  <button
                    v-if="nextStatus(task.status)"
                    type="button"
                    class="ghost-btn"
                    :disabled="saving || isPreview"
                    @click="startProgress(task)"
                  >
                    推进任务
                  </button>
                  <span v-else class="finished-text">已完成</span>
                </td>
              </tr>
              <tr v-if="progressForm.taskId === task.taskId" class="progress-row">
                <td colspan="7">
                  <form class="progress-form" @submit.prevent="advanceStatus(task)">
                    <strong>更新为“{{ nextStatus(task.status) }}”</strong>
                    <label class="progress-remark">
                      巡检备注
                      <input v-model="progressForm.remark" type="text" maxlength="200" placeholder="可补充本次巡检情况" />
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
.inspection-page { display: grid; gap: 20px; }
.panel { padding: 24px; border-radius: var(--tj-radius); background: var(--tj-card-bg); box-shadow: var(--tj-shadow); }
.panel h2 { margin: 0; font-size: 20px; color: var(--tj-text); }
.preview-banner, .notice-banner { margin: 0; padding: 12px 16px; border-radius: 12px; }
.preview-banner { background: #fff7ed; color: #c2410c; }
.notice-banner { background: #e8f7ee; color: #15803d; }
.inline-error { margin: 0; color: var(--tj-danger); }
.create-form, .progress-form { display: flex; gap: 12px; align-items: end; flex-wrap: wrap; }
.create-form { margin-top: 18px; }
.create-form label, .progress-form label, .filter-field { display: grid; gap: 6px; color: #2a3c59; font-size: 14px; }
.remark-field { flex: 1; min-width: 220px; }
input, select { min-height: 40px; padding: 8px 11px; border: 1px solid #d7e0ef; border-radius: 9px; background: #fff; color: var(--tj-text); }
.panel-heading { display: flex; justify-content: space-between; gap: 16px; align-items: end; margin-bottom: 18px; }
.table-wrap { overflow-x: auto; }
table { width: 100%; border-collapse: collapse; }
th, td { padding: 12px 10px; border-bottom: 1px solid #edf1f7; text-align: left; vertical-align: middle; font-size: 14px; }
th { color: var(--tj-text-muted); font-size: 13px; white-space: nowrap; }
.remark-cell { min-width: 180px; max-width: 320px; }
.status-tag { display: inline-block; padding: 4px 10px; border-radius: 999px; font-size: 12px; white-space: nowrap; }
.status-tag.waiting { background: #fff7ed; color: #c2410c; }
.status-tag.working { background: #e8f0ff; color: #285cff; }
.status-tag.done { background: #e8f7ee; color: #15803d; }
.progress-row td { background: #f7f9fd; }
.progress-form { justify-content: flex-end; }
.progress-remark { flex: 1; min-width: 240px; }
.primary-btn, .ghost-btn { min-height: 38px; padding: 8px 14px; border-radius: 9px; font-weight: 600; cursor: pointer; }
.primary-btn { border: 0; background: var(--tj-primary); color: #fff; }
.ghost-btn { border: 1px solid #d7e0ef; background: #fff; color: #2a3c59; }
.primary-btn:disabled, .ghost-btn:disabled { opacity: .6; cursor: not-allowed; }
.finished-text, .empty-text { color: var(--tj-text-muted); }
.empty-text { margin: 24px 0 0; text-align: center; }
@media (max-width: 760px) { .panel-heading { align-items: stretch; flex-direction: column; } .create-form { align-items: stretch; flex-direction: column; } }
</style>
