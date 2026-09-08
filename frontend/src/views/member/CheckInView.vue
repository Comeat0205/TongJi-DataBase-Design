<script setup lang="ts">
// E - 会员签到 & 签退
import { onMounted, ref } from 'vue'
import { ApiError } from '@/api/http'
import { checkIn, checkOut, getMyCheckIn, getVenueStatus, type CheckInOutRecord, type VenueStatus } from '@/api/check-in-out'
import { useAuthStore } from '@/stores/auth'
import PageHeader from '@/components/ui/PageHeader.vue'

const auth = useAuthStore()
const myUserId = auth.session?.userId ?? 0

const venueId = ref(1)
const loading = ref(false)
const errMsg = ref('')
const result = ref<any>(null)
const venues = ref<VenueStatus[]>([])
const myCheckIn = ref<CheckInOutRecord | null>(null)

onMounted(async () => {
  try {
    venues.value = await getVenueStatus()
    if (venues.value.length > 0) {
      venueId.value = venues.value[0]!.venueId
    }
  } catch { /* ignore */ }
  // 自动查询当前在场状态
  if (myUserId) {
    try { myCheckIn.value = await getMyCheckIn(myUserId) } catch { myCheckIn.value = null }
  }
})

const curVenue = () => venues.value.find(v => v.venueId === venueId.value)

function barClass(v: VenueStatus) {
  if (v.occupancyRate >= 100) return 'full'
  if (v.occupancyRate >= 80) return 'warn'
  return ''
}

function fmtTime(v?: string) {
  return v ? new Date(v).toLocaleString('zh-CN') : '-'
}

async function doCheckIn() {
  if (!myUserId) { errMsg.value = '未登录'; return }
  loading.value = true; errMsg.value = ''; result.value = null
  try {
    result.value = await checkIn({ cardId: myUserId, venueId: venueId.value })
    venues.value = await getVenueStatus()
    try { myCheckIn.value = await getMyCheckIn(myUserId) } catch { myCheckIn.value = null }
  } catch (e) {
    errMsg.value = e instanceof ApiError ? e.message : '签到失败'
  } finally { loading.value = false }
}

async function doCheckOut() {
  if (!myCheckIn.value) return
  loading.value = true; errMsg.value = ''
  try {
    await checkOut(myCheckIn.value.checkInOutId)
    myCheckIn.value = null; result.value = null
    venues.value = await getVenueStatus()
  } catch (e) {
    errMsg.value = e instanceof ApiError ? e.message : '退场失败'
  } finally { loading.value = false }
}
</script>

<template>
  <div class="check-in-page">
    <PageHeader title="签到签退" subtitle="刷会员卡签到/签退，自动校验有效期 / 次数" />

    <!-- 场馆容量 -->
    <div v-if="venues.length" class="venue-bar">
      <div
        v-for="v in venues" :key="v.venueId"
        class="venue-chip"
        :class="{ active: v.venueId === venueId, warn: v.occupancyRate >= 90 && v.occupancyRate < 100, full: v.occupancyRate >= 100 }"
        @click="venueId = v.venueId"
      >
        <b>{{ v.venueName }}</b>
        <span class="cap">{{ v.currentCapacity }}/{{ v.maxCapacity }}</span>
        <span class="rate">{{ v.occupancyRate }}%</span>
      </div>
    </div>

    <!-- 两张主卡片：签到 + 签退 -->
    <div class="two-cards">
      <!-- 左：签到 -->
      <div class="card checkin-card">
        <h2>签到</h2>
        <p class="card-desc">选择场馆后签到入场，卡号自动关联</p>
        <div class="card-id-display">
          <span class="label">会员卡编号</span>
          <span class="value">{{ myUserId }}</span>
        </div>
        <form @submit.prevent="doCheckIn">
          <label v-if="venues.length > 1" class="field">
            <span>选择场馆</span>
            <select v-model.number="venueId" :disabled="loading">
              <option v-for="v in venues" :key="v.venueId" :value="v.venueId">{{ v.venueName }}</option>
            </select>
          </label>
          <button type="submit" class="btn-primary" :disabled="loading || !!myCheckIn">
            {{ loading ? '处理中...' : (myCheckIn ? '已在场中，无法重复签到' : '确认签到') }}
          </button>
        </form>
        <p v-if="errMsg" class="err">{{ errMsg }}</p>
      </div>

      <!-- 右：签退 -->
      <div class="card checkout-card" :class="{ 'is-active': !!myCheckIn }">
        <h2>签退</h2>
        <template v-if="myCheckIn">
          <p class="card-desc in-field">你当前在场内</p>
          <div class="kv">
            <div class="row"><span>场馆</span><b>{{ myCheckIn.venueName }}</b></div>
            <div class="row"><span>入场时间</span><b>{{ fmtTime(myCheckIn.checkInTime) }}</b></div>
          </div>
          <button class="btn-checkout" :disabled="loading" @click="doCheckOut">
            {{ loading ? '处理中...' : '确认签退' }}
          </button>
        </template>
        <template v-else>
          <div class="not-in-field">
            <div class="idle-icon">🚪</div>
            <p>你目前不在场</p>
            <span class="hint">签到后即可在此签退</span>
          </div>
        </template>
      </div>
    </div>

    <!-- 入场成功详情 -->
    <div v-if="result" class="card result">
      <p class="ok-badge">签到成功</p>
      <div class="kv">
        <div class="row"><span>会员</span><b>{{ result.memberName }}</b></div>
        <div class="row"><span>场馆</span><b>{{ result.venueName }}</b></div>
        <div class="row"><span>入场时间</span><b>{{ fmtTime(result.checkInTime) }}</b></div>
        <div class="row"><span>卡类型</span><b>{{ result.cardType }}</b></div>
        <div v-if="result.remainingCount != null" class="row">
          <span>剩余次数</span><b>{{ result.remainingCount }}</b>
        </div>
        <div v-if="result.expireDate" class="row">
          <span>有效期至</span><b>{{ new Date(result.expireDate).toLocaleDateString('zh-CN') }}</b>
        </div>
      </div>
    </div>

    <!-- 容量条 -->
    <div v-if="curVenue()" class="card">
      <h3>{{ curVenue()!.venueName }} 实时容量</h3>
      <div class="bar-bg">
        <div class="bar" :style="{ width: Math.min(curVenue()!.occupancyRate, 100) + '%' }" :class="barClass(curVenue()!)" />
      </div>
      <p class="bar-text">{{ curVenue()!.currentCapacity }} / {{ curVenue()!.maxCapacity }} ({{ curVenue()!.occupancyRate }}%)</p>
    </div>
  </div>
</template>

<style scoped>
.check-in-page { display: grid; gap: 20px; }

.venue-bar { display: flex; gap: 12px; flex-wrap: wrap; }
.venue-chip {
  padding: 10px 16px; border-radius: 12px; background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow); display: flex; flex-direction: column; gap: 2px; cursor: pointer;
}
.venue-chip.active { border: 2px solid #4d77ff; }
.venue-chip.warn { border-color: #faad14; background: #fffbe6; }
.venue-chip.full { border-color: #ff4d4f; background: #fff1f0; }
.venue-chip b { font-size: 14px; }
.venue-chip .cap { font-size: 13px; color: #7a88a0; }
.venue-chip .rate { font-size: 12px; color: #2c57d2; font-weight: 600; }
.venue-chip.warn .rate { color: #d48806; }
.venue-chip.full .rate { color: #cf1322; }

.two-cards {
  display: grid; grid-template-columns: 1fr 1fr; gap: 20px;
}
@media (max-width: 720px) {
  .two-cards { grid-template-columns: 1fr; }
}

.card {
  padding: 24px; border-radius: var(--tj-radius);
  background: var(--tj-card-bg); box-shadow: var(--tj-shadow);
}
.card h2 { margin: 0 0 8px; font-size: 20px; }
.card-desc { color: #7a88a0; font-size: 14px; margin: 0 0 16px; }
.card-id-display {
  display: flex; justify-content: space-between; align-items: center;
  padding: 10px 14px; border-radius: 10px; background: #f5f7fa;
  margin-bottom: 16px;
}
.card-id-display .label { color: #7a88a0; font-size: 14px; }
.card-id-display .value { font-size: 16px; font-weight: 600; color: #1a2332; }
.card form { display: grid; gap: 14px; max-width: 400px; }

.field { display: grid; gap: 6px; }
.field span { color: #7a88a0; font-size: 14px; }
.field input, .field select {
  padding: 10px 12px; border: 1px solid #d8e2f0; border-radius: 10px;
  font-size: 15px; background: #fff;
}
.field input:focus, .field select:focus { outline: none; border-color: #4d77ff; }

.btn-primary {
  padding: 10px 20px; border: none; border-radius: 10px;
  background: #285cff; color: #fff; font-size: 15px; font-weight: 600; cursor: pointer;
}
.btn-primary:disabled { opacity: .5; cursor: not-allowed; }

.checkout-card.is-active { border: 2px solid #91caff; }

.btn-checkout {
  margin-top: 16px; padding: 10px 20px; border: 2px solid #cf1322; border-radius: 10px;
  background: #fff; color: #cf1322; font-size: 15px; font-weight: 600; cursor: pointer; width: 100%;
}
.btn-checkout:hover { background: #fff1f0; }
.btn-checkout:disabled { opacity: .5; cursor: not-allowed; }

.not-in-field {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  padding: 32px 0; color: #7a88a0; text-align: center;
}
.idle-icon { font-size: 36px; margin-bottom: 12px; opacity: .6; }
.not-in-field p { margin: 0; font-size: 16px; font-weight: 500; color: #5a6577; }
.hint { font-size: 13px; margin-top: 6px; }

.err { color: #cf1322; margin: 12px 0 0; font-size: 14px; }

.ok-badge {
  display: inline-block; padding: 4px 12px; border-radius: 20px;
  background: #e6fff4; color: #0a8a4a; font-size: 13px; font-weight: 600; margin: 0 0 12px;
}

.kv { display: grid; gap: 10px; }
.row { display: flex; justify-content: space-between; padding-bottom: 8px; border-bottom: 1px solid #eef2f7; }
.row:last-child { border: none; padding-bottom: 0; }
.row span { color: #7a88a0; }

.bar-bg { height: 10px; border-radius: 5px; background: #eef2f7; overflow: hidden; }
.bar { height: 100%; border-radius: 5px; background: #285cff; transition: width .3s; }
.bar.warn { background: #ff9c00; }
.bar.full { background: #ff4d4f; }
.bar-text { margin: 6px 0 0; font-size: 13px; color: #7a88a0; }
</style>
