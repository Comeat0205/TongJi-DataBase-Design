<script setup lang="ts">
// E - 员工前台入场 & 退场
import { onMounted, ref } from 'vue'
import { ApiError } from '@/api/http'
import { checkIn, checkOut, getActiveCheckIns, getVenueStatus } from '@/api/check-in-out'
import PageHeader from '@/components/ui/PageHeader.vue'

const venues = ref<any[]>([])
const curVenueId = ref(1)
const activeList = ref<any[]>([])
const loading = ref(false)
const msg = ref('') // 成功提示
const err = ref('')

const cardInput = ref<number | ''>('')
const submitting = ref(false)

onMounted(async () => {
  await refreshVenues()
  if (curVenueId.value) await refreshActive()
})

async function refreshVenues() {
  try {
    venues.value = await getVenueStatus()
    if (venues.value.length > 0 && !curVenueId.value) {
      curVenueId.value = venues.value[0]!.venueId
    }
  } catch { /* 不管 */ }
}

async function refreshActive() {
  loading.value = true
  try {
    activeList.value = await getActiveCheckIns(curVenueId.value)
  } catch {
    activeList.value = []
  } finally {
    loading.value = false
  }
}

function fmt(v?: string) { return v ? new Date(v).toLocaleString('zh-CN') : '-' }
function cur() { return venues.value.find((v: any) => v.venueId === curVenueId.value) }

async function doCheckIn() {
  if (!cardInput.value) { err.value = '输入卡编号'; return }
  submitting.value = true; err.value = ''; msg.value = ''
  try {
    const res = await checkIn({ cardId: Number(cardInput.value), venueId: curVenueId.value })
    msg.value = `${res.memberName} 入场成功`
    cardInput.value = ''
    await refreshActive()
    await refreshVenues()
  } catch (e) {
    err.value = e instanceof ApiError ? e.message : '入场失败'
  } finally { submitting.value = false }
}

async function doCheckOut(id: number) {
  err.value = ''; msg.value = ''
  try {
    const res = await checkOut(id)
    if (res) msg.value = `${res.memberName || '会员'} 已退场`
    await refreshActive()
    await refreshVenues()
  } catch (e) {
    err.value = e instanceof ApiError ? e.message : '退场失败'
  }
}

function switchVenue(id: number) {
  curVenueId.value = id
  refreshActive()
}
</script>

<template>
  <div class="desk-page">
    <PageHeader title="前台入场" subtitle="员工办理入场 / 退场，查看在场人员" />

    <!-- 场馆 tab -->
    <div class="venue-bar">
      <button
        v-for="v in venues" :key="v.venueId"
        class="vtab" :class="{ active: v.venueId === curVenueId }"
        @click="switchVenue(v.venueId)"
      >
        {{ v.venueName }}
        <small>{{ v.currentCapacity }}/{{ v.maxCapacity }}</small>
      </button>
      <span v-if="cur()" class="venue-info" :class="{ warn: cur()!.occupancyRate >= 80 }">
        {{ cur()!.venueStatus }} · {{ cur()!.currentCapacity }}/{{ cur()!.maxCapacity }}
        ({{ cur()!.occupancyRate }}%)
      </span>
    </div>

    <!-- 入场 -->
    <div class="card">
      <h2>办理入场</h2>
      <form class="inline" @submit.prevent="doCheckIn">
        <input v-model.number="cardInput" type="number" min="1" placeholder="会员卡编号" :disabled="submitting" />
        <button type="submit" class="btn-primary" :disabled="submitting">
          {{ submitting ? '...' : '入场' }}
        </button>
      </form>
      <p v-if="msg" class="ok">{{ msg }}</p>
      <p v-if="err" class="err">{{ err }}</p>
    </div>

    <!-- 在场列表 -->
    <div class="card">
      <h2>在场人员 ({{ activeList.length }})</h2>

      <p v-if="loading" class="muted">加载中...</p>
      <p v-else-if="!activeList.length" class="muted">暂无在场人员</p>

      <table v-else>
        <thead>
          <tr><th>ID</th><th>会员卡</th><th>姓名</th><th>入场时间</th><th></th></tr>
        </thead>
        <tbody>
          <tr v-for="r in activeList" :key="r.checkInOutId">
            <td>#{{ r.checkInOutId }}</td>
            <td>{{ r.cardId ?? '-' }}</td>
            <td>{{ r.memberName ?? '-' }}</td>
            <td>{{ fmt(r.checkInTime) }}</td>
            <td><button class="btn-sm" @click="doCheckOut(r.checkInOutId)">退场</button></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.desk-page { display: grid; gap: 20px; }

.venue-bar {
  display: flex; gap: 8px; flex-wrap: wrap; align-items: center;
  padding: 16px 20px; border-radius: var(--tj-radius);
  background: var(--tj-card-bg); box-shadow: var(--tj-shadow);
}
.vtab {
  padding: 8px 14px; border: 1px solid #d8e2f0; border-radius: 8px;
  background: #fff; cursor: pointer; font-size: 14px;
  display: flex; flex-direction: column; align-items: center; gap: 2px;
}
.vtab.active { border-color: #4d77ff; background: #f0f5ff; color: #2c57d2; }
.vtab small { font-size: 11px; color: #999; }
.venue-info { margin-left: auto; font-size: 13px; color: #7a88a0; }
.venue-info.warn { color: #d46b08; font-weight: 600; }

.card {
  padding: 24px; border-radius: var(--tj-radius);
  background: var(--tj-card-bg); box-shadow: var(--tj-shadow);
}
.card h2 { margin: 0 0 14px; font-size: 18px; }

.inline { display: flex; gap: 10px; max-width: 380px; }
.inline input {
  flex: 1; padding: 8px 12px; border: 1px solid #d8e2f0;
  border-radius: 8px; font-size: 14px;
}
.inline input:focus { outline: none; border-color: #4d77ff; }

.btn-primary {
  padding: 8px 16px; border: none; border-radius: 8px;
  background: #285cff; color: #fff; font-weight: 600; cursor: pointer; white-space: nowrap;
}
.btn-primary:disabled { opacity: .5; }

.btn-sm {
  padding: 4px 10px; border: 1px solid #d8e2f0; border-radius: 6px;
  background: #fff; font-size: 12px; cursor: pointer;
}
.btn-sm:hover { background: #f5f8ff; }

.ok { color: #0a8a4a; margin: 10px 0 0; font-size: 14px; }
.err { color: #cf1322; margin: 10px 0 0; font-size: 14px; }
.muted { color: #999; padding: 16px 0; text-align: center; }

table { width: 100%; border-collapse: collapse; }
th { text-align: left; padding: 8px 10px; border-bottom: 2px solid #eef2f7; color: #7a88a0; font-size: 13px; }
td { padding: 10px; border-bottom: 1px solid #eef2f7; font-size: 14px; }
tr:last-child td { border-bottom: none; }
</style>
