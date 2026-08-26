<script setup lang="ts">
// E - 会员入场签到
import { onMounted, ref } from 'vue'
import { ApiError } from '@/api/http'
import { checkIn, getVenueStatus, type VenueStatus } from '@/api/check-in-out'
import PageHeader from '@/components/ui/PageHeader.vue'

const cardId = ref<number | ''>('')
const venueId = ref(1)
const loading = ref(false)
const errMsg = ref('')
const result = ref<any>(null) // 入场成功后的返回
const venues = ref<VenueStatus[]>([])

onMounted(async () => {
  try {
    venues.value = await getVenueStatus()
    if (venues.value.length > 0) {
      venueId.value = venues.value[0]!.venueId
    }
  } catch {
    // ignore, 场馆列表挂了不影响入场
  }
})

const curVenue = () => venues.value.find(v => v.venueId === venueId.value)

function fmtTime(v?: string) {
  return v ? new Date(v).toLocaleString('zh-CN') : '-'
}

async function doCheckIn() {
  if (!cardId.value) {
    errMsg.value = '请输入卡编号'
    return
  }
  loading.value = true
  errMsg.value = ''
  result.value = null

  try {
    result.value = await checkIn({
      cardId: Number(cardId.value),
      venueId: venueId.value,
    })
    cardId.value = ''
    // 刷一下容量
    venues.value = await getVenueStatus()
  } catch (e) {
    errMsg.value = e instanceof ApiError ? e.message : '入场失败'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="check-in-page">
    <PageHeader title="入场签到" subtitle="刷会员卡入场，自动校验有效期 / 次数" />

    <!-- 场馆容量 -->
    <div v-if="venues.length" class="venue-bar">
      <div
        v-for="v in venues"
        :key="v.venueId"
        class="venue-chip"
        :class="{ active: v.venueId === venueId, full: v.occupancyRate >= 90 }"
      >
        <b>{{ v.venueName }}</b>
        <span class="cap">{{ v.currentCapacity }}/{{ v.maxCapacity }}</span>
        <span class="rate">{{ v.occupancyRate }}%</span>
      </div>
    </div>

    <!-- 入场表单 -->
    <div class="card">
      <h2>刷卡入场</h2>
      <form @submit.prevent="doCheckIn">
        <label class="field">
          <span>会员卡编号</span>
          <input v-model.number="cardId" type="number" min="1" placeholder="输入卡 ID" :disabled="loading" />
        </label>

        <label v-if="venues.length > 1" class="field">
          <span>选择场馆</span>
          <select v-model.number="venueId" :disabled="loading">
            <option v-for="v in venues" :key="v.venueId" :value="v.venueId">{{ v.venueName }}</option>
          </select>
        </label>

        <button type="submit" class="btn-primary" :disabled="loading">
          {{ loading ? '处理中...' : '确认入场' }}
        </button>
      </form>

      <p v-if="errMsg" class="err">{{ errMsg }}</p>
    </div>

    <!-- 入场成功 -->
    <div v-if="result" class="card result">
      <p class="ok-badge">入场成功</p>
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
        <div
          class="bar"
          :style="{ width: Math.min(curVenue()!.occupancyRate, 100) + '%' }"
          :class="{ warn: curVenue()!.occupancyRate >= 80 }"
        />
      </div>
      <p class="bar-text">
        {{ curVenue()!.currentCapacity }} / {{ curVenue()!.maxCapacity }}
        ({{ curVenue()!.occupancyRate }}%)
      </p>
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
.venue-chip.full { border-color: #ff4d4f; }
.venue-chip b { font-size: 14px; }
.venue-chip .cap { font-size: 13px; color: #7a88a0; }
.venue-chip .rate { font-size: 12px; color: #2c57d2; font-weight: 600; }

.card {
  padding: 24px; border-radius: var(--tj-radius);
  background: var(--tj-card-bg); box-shadow: var(--tj-shadow);
}
.card h2, .card h3 { margin: 0 0 16px; font-size: 20px; }
.card form { display: grid; gap: 14px; max-width: 400px; }

.field { display: grid; gap: 6px; }
.field span { color: #7a88a0; font-size: 14px; }
.field input, .field select {
  padding: 10px 12px; border: 1px solid #d8e2f0; border-radius: 10px;
  font-size: 15px; background: #fff;
}
.field input:focus, .field select:focus {
  outline: none; border-color: #4d77ff;
}

.btn-primary {
  padding: 10px 20px; border: none; border-radius: 10px;
  background: #285cff; color: #fff; font-size: 15px; font-weight: 600; cursor: pointer;
}
.btn-primary:disabled { opacity: .5; cursor: not-allowed; }

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
.bar-text { margin: 6px 0 0; font-size: 13px; color: #7a88a0; }
</style>
