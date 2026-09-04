<script setup lang="ts">
// E - 容量日志查看 (#7 #21)
import { onMounted, ref, watch } from 'vue'
import { ApiError } from '@/api/http'
import { getCapacityLogs } from '@/api/capacity-logs'
import { getVenueStatus } from '@/api/check-in-out'
import PageHeader from '@/components/ui/PageHeader.vue'

const venues = ref<any[]>([])
const filterVenue = ref(0) // 0=全部
const logs = ref<any[]>([])
const loading = ref(false)
const page = ref(1)
const PAGE_SIZE = 20

onMounted(async () => {
  try { venues.value = await getVenueStatus() } catch { /* */ }
  await load()
})

watch(filterVenue, () => { page.value = 1; load() })

async function load() {
  loading.value = true
  try {
    logs.value = await getCapacityLogs(filterVenue.value, page.value, PAGE_SIZE)
  } catch (e) {
    // TODO: 错误提示还没接
    console.warn('load capacity logs failed', e)
  } finally {
    loading.value = false
  }
}

function fmt(v?: string) { return v ? new Date(v).toLocaleString('zh-CN') : '-' }

// 占用率颜色
function rateColor(r?: number) {
  if (r == null) return '#999'
  if (r >= 90) return '#cf1322'
  if (r >= 70) return '#d46b08'
  return '#0a8a4a'
}
</script>

<template>
  <div class="logs-page">
    <PageHeader title="容量日志" subtitle="各场馆历史容量快照，23:00 自动签退后自动生成" />

    <!-- 筛选 -->
    <div class="toolbar">
      <select v-model.number="filterVenue">
        <option :value="0">全部场馆</option>
        <option v-for="v in venues" :key="v.venueId" :value="v.venueId">{{ v.venueName }}</option>
      </select>

      <div class="pager">
        <button :disabled="page <= 1" @click="page--; load()">上一页</button>
        <span>第 {{ page }} 页</span>
        <button :disabled="logs.length < PAGE_SIZE" @click="page++; load()">下一页</button>
      </div>
    </div>

    <p v-if="loading" class="muted">加载中...</p>

    <div v-else class="card">
      <p v-if="!logs.length" class="muted">暂无记录</p>
      <table v-else>
        <thead>
          <tr>
            <th>ID</th><th>场馆</th><th>时间</th><th>最大容量</th><th>在场</th><th>占用率</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="l in logs" :key="l.capacityLogId">
            <td>#{{ l.capacityLogId }}</td>
            <td>{{ l.venueName }}</td>
            <td>{{ fmt(l.logTimestamp) }}</td>
            <td>{{ l.recordedCapacity ?? '-' }}</td>
            <td>{{ l.recordedCount }}</td>
            <td :style="{ color: rateColor(l.occupancyRate), fontWeight: 600 }">
              {{ l.occupancyRate != null ? l.occupancyRate + '%' : '-' }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.logs-page { display: grid; gap: 16px; }

.toolbar {
  display: flex; justify-content: space-between; align-items: center;
  padding: 12px 20px; border-radius: var(--tj-radius);
  background: var(--tj-card-bg); box-shadow: var(--tj-shadow);
}
.toolbar select {
  padding: 6px 10px; border: 1px solid #d8e2f0; border-radius: 8px;
  font-size: 14px; min-width: 140px;
}
.pager { display: flex; align-items: center; gap: 10px; }
.pager button {
  padding: 4px 12px; border: 1px solid #d8e2f0; border-radius: 6px;
  background: #fff; font-size: 13px; cursor: pointer;
}
.pager button:disabled { opacity: .4; cursor: not-allowed; }
.pager span { font-size: 13px; color: #999; }

.card {
  padding: 20px; border-radius: var(--tj-radius);
  background: var(--tj-card-bg); box-shadow: var(--tj-shadow);
}
.muted { color: #999; text-align: center; padding: 20px 0; }

table { width: 100%; border-collapse: collapse; }
th { text-align: left; padding: 8px 10px; border-bottom: 2px solid #eef2f7; color: #7a88a0; font-size: 13px; }
td { padding: 10px; border-bottom: 1px solid #eef2f7; font-size: 14px; }
tr:last-child td { border-bottom: none; }
</style>
