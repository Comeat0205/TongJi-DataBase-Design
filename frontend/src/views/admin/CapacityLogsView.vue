<script setup lang="ts">
// E - 主训练馆容量波形（每 10 分钟采样，按日查看）
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import {
  getDailyCapacityMovements,
  getDailyCapacitySeries,
  type CapacityDailySeries,
  type CapacityLogPoint,
  type CapacityMovement,
} from '@/api/capacity-logs'
import PageHeader from '@/components/ui/PageHeader.vue'

const series = ref<CapacityDailySeries | null>(null)
const movements = ref<CapacityMovement[]>([])
const loading = ref(false)
const errorMsg = ref('')
const selectedDate = ref(todayStr())

const W = 720
const H = 360
const PAD = { top: 28, right: 24, bottom: 40, left: 52 }

function todayStr() {
  const d = new Date()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${d.getFullYear()}-${m}-${day}`
}

function minutesFromMidnight(label: string) {
  const [hh, mm] = label.split(':').map(Number)
  return (hh || 0) * 60 + (mm || 0)
}

/** 一天 0:00–23:50，共 144 个十分钟槽位 */
const SLOT_COUNT = 24 * 6

/** 取「好看」的纵轴上限，便于刻度均匀 */
function niceCeil(n: number): number {
  if (n <= 0) return 1
  const exp = 10 ** Math.floor(Math.log10(n))
  const frac = n / exp
  const nice = frac <= 1 ? 1 : frac <= 2 ? 2 : frac <= 5 ? 5 : 10
  return nice * exp
}

function buildNiceTicks(max: number, targetCount = 5): number[] {
  if (max <= 0) return [0]
  const rough = max / Math.max(targetCount - 1, 1)
  const step = niceCeil(rough)
  const ticks: number[] = []
  for (let v = 0; v <= max + 1e-9; v += step) {
    ticks.push(Math.round(v * 1000) / 1000)
  }
  if (ticks[ticks.length - 1] !== max) ticks.push(max)
  return ticks
}

function xOfMinutes(mins: number) {
  const inner = W - PAD.left - PAD.right
  return PAD.left + (mins / (24 * 60 - 10)) * inner
}

/** 线性纵轴：0 → max 均匀映射到绘图区高度 */
function yOf(value: number, max: number) {
  const inner = H - PAD.top - PAD.bottom
  if (max <= 0) return PAD.top + inner
  const v = Math.min(Math.max(value, 0), max)
  return PAD.top + inner - (v / max) * inner
}

function buildPolyline(
  points: CapacityLogPoint[],
  valueOf: (p: CapacityLogPoint) => number,
  max: number,
) {
  if (!points.length || max <= 0) return ''
  return points
    .map((p) => {
      const x = xOfMinutes(minutesFromMidnight(p.timeLabel))
      const y = yOf(valueOf(p), max)
      return `${x.toFixed(1)},${y.toFixed(1)}`
    })
    .join(' ')
}

function buildAreaPath(
  points: CapacityLogPoint[],
  valueOf: (p: CapacityLogPoint) => number,
  max: number,
) {
  if (!points.length || max <= 0) return ''
  const baseY = H - PAD.bottom
  const coords = points.map((p) => {
    const x = xOfMinutes(minutesFromMidnight(p.timeLabel))
    const y = yOf(valueOf(p), max)
    return { x, y }
  })
  const first = coords[0]!
  const last = coords[coords.length - 1]!
  const line = coords.map((c) => `${c.x.toFixed(1)},${c.y.toFixed(1)}`).join(' ')
  return `M ${first.x.toFixed(1)},${baseY.toFixed(1)} L ${line} L ${last.x.toFixed(1)},${baseY.toFixed(1)} Z`
}

const peakCount = computed(
  () => series.value?.points.reduce((m, p) => Math.max(m, p.recordedCount), 0) ?? 0,
)
const peakRate = computed(
  () => series.value?.points.reduce((m, p) => Math.max(m, Number(p.occupancyRate) || 0), 0) ?? 0,
)

/** 人数纵轴：按当日峰值自适应；接近满员时拉到场馆上限 */
const countAxisMax = computed(() => {
  const maxCap = series.value?.maxCapacity ?? 0
  const peak = peakCount.value
  if (peak <= 0) {
    const fallback = maxCap > 0 ? Math.min(niceCeil(maxCap * 0.15), maxCap) : 10
    return Math.max(fallback, 5)
  }
  if (maxCap > 0 && peak >= maxCap * 0.7) return maxCap
  const padded = niceCeil(peak * 1.2)
  if (maxCap > 0) return Math.min(Math.max(padded, 5), maxCap)
  return Math.max(padded, 5)
})

/** 占用率纵轴：低占用时收缩，高占用时拉满 100% */
const rateAxisMax = computed(() => {
  const peak = peakRate.value
  if (peak <= 0) return 20
  if (peak >= 70) return 100
  return Math.min(100, Math.max(niceCeil(peak * 1.25), 10))
})

const countPolyline = computed(() =>
  buildPolyline(series.value?.points ?? [], (p) => p.recordedCount, countAxisMax.value),
)
const ratePolyline = computed(() =>
  buildPolyline(series.value?.points ?? [], (p) => Number(p.occupancyRate) || 0, rateAxisMax.value),
)
const countArea = computed(() =>
  buildAreaPath(series.value?.points ?? [], (p) => p.recordedCount, countAxisMax.value),
)
const rateArea = computed(() =>
  buildAreaPath(series.value?.points ?? [], (p) => Number(p.occupancyRate) || 0, rateAxisMax.value),
)

const countYTicks = computed(() =>
  buildNiceTicks(countAxisMax.value).map((v) => ({
    label: String(Math.round(v)),
    y: yOf(v, countAxisMax.value),
    value: v,
  })),
)

const rateYTicks = computed(() =>
  buildNiceTicks(rateAxisMax.value).map((v) => ({
    label: `${Math.round(v)}%`,
    y: yOf(v, rateAxisMax.value),
    value: v,
  })),
)

const hourTicks = computed(() =>
  Array.from({ length: 12 }, (_, i) => {
    const hour = i * 2
    return { label: `${String(hour).padStart(2, '0')}:00`, x: xOfMinutes(hour * 60) }
  }),
)

type TipKind = 'count' | 'rate'
const tip = ref<{
  kind: TipKind
  timeLabel: string
  count: number
  rate: number
  x: number
  y: number
} | null>(null)

function showTip(kind: TipKind, p: CapacityLogPoint, event: MouseEvent) {
  const card = (event.currentTarget as Element).closest('.chart-card') as HTMLElement | null
  if (!card) return
  const rect = card.getBoundingClientRect()
  tip.value = {
    kind,
    timeLabel: p.timeLabel,
    count: p.recordedCount,
    rate: Number(p.occupancyRate),
    x: event.clientX - rect.left,
    y: event.clientY - rect.top,
  }
}

function moveTip(event: MouseEvent) {
  if (!tip.value) return
  const card = (event.currentTarget as Element).closest('.chart-card') as HTMLElement | null
  if (!card) return
  const rect = card.getBoundingClientRect()
  tip.value = {
    ...tip.value,
    x: event.clientX - rect.left,
    y: event.clientY - rect.top,
  }
}

function hideTip() {
  tip.value = null
}

async function load() {
  loading.value = true
  errorMsg.value = ''
  try {
    const [s, m] = await Promise.all([
      getDailyCapacitySeries(selectedDate.value),
      getDailyCapacityMovements(selectedDate.value),
    ])
    series.value = s
    movements.value = m
  } catch (e: any) {
    series.value = null
    movements.value = []
    errorMsg.value = e?.message || '加载失败'
  } finally {
    loading.value = false
  }
}

function fmtTime(v?: string) {
  if (!v) return '-'
  return new Date(v).toLocaleString('zh-CN', { hour12: false })
}

watch(selectedDate, () => {
  void load()
})

let timer: number | undefined
onMounted(async () => {
  await load()
  timer = window.setInterval(() => {
    if (selectedDate.value === todayStr()) void load()
  }, 60_000)
})
onUnmounted(() => {
  if (timer) window.clearInterval(timer)
})
</script>

<template>
  <div class="logs-page">
    <PageHeader
      title="容量日志"
      :subtitle="series ? `${series.venueName} · 从 0:00 起每 10 分钟采样 · 按日波形` : '主训练馆实时人数与占用率波形'"
    />

    <div class="charts-wrap">
      <p v-if="loading" class="muted">加载中...</p>
      <p v-else-if="errorMsg" class="error">{{ errorMsg }}</p>

      <template v-else>
        <p v-if="!series?.points?.length" class="muted hint">
          {{ selectedDate }} 暂无采样点。服务运行后会每 10 分钟自动写入；也可稍候刷新。
        </p>

        <section class="chart-card">
          <div class="chart-head">
            <h3>实时在场人数</h3>
            <span class="meta">
              上限 {{ series?.maxCapacity ?? '-' }} · 当日峰值 {{ peakCount }} · 纵轴 0–{{ countAxisMax }}（自适应）
            </span>
          </div>
          <div class="chart-stage">
            <svg class="chart" :viewBox="`0 0 ${W} ${H}`" role="img" aria-label="在场人数波形">
              <defs>
                <linearGradient id="countFill" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stop-color="#1d4f91" stop-opacity="0.28" />
                  <stop offset="100%" stop-color="#1d4f91" stop-opacity="0.02" />
                </linearGradient>
              </defs>

              <line
                v-for="t in countYTicks"
                :key="'hg-c-' + t.label"
                class="grid-h"
                :x1="PAD.left"
                :x2="W - PAD.right"
                :y1="t.y"
                :y2="t.y"
              />
              <line
                v-for="t in hourTicks"
                :key="'gc-' + t.label"
                class="grid-v"
                :x1="t.x"
                :x2="t.x"
                :y1="PAD.top"
                :y2="H - PAD.bottom"
              />

              <line class="axis" :x1="PAD.left" :y1="H - PAD.bottom" :x2="W - PAD.right" :y2="H - PAD.bottom" />
              <line class="axis" :x1="PAD.left" :y1="PAD.top" :x2="PAD.left" :y2="H - PAD.bottom" />

              <text
                v-for="t in countYTicks"
                :key="'cy-' + t.label"
                class="tick"
                :x="PAD.left - 8"
                :y="t.y + 4"
                text-anchor="end"
              >
                {{ t.label }}
              </text>
              <text
                v-for="t in hourTicks"
                :key="'lt-' + t.label"
                class="tick"
                :x="t.x"
                :y="H - 12"
                text-anchor="middle"
              >
                {{ t.label }}
              </text>

              <path v-if="countArea" class="area" :d="countArea" fill="url(#countFill)" />
              <polyline v-if="countPolyline" class="wave count" fill="none" :points="countPolyline" />

              <g
                v-for="(p, i) in series?.points ?? []"
                :key="'c-' + i"
                class="dot-hit"
                @mouseenter="showTip('count', p, $event)"
                @mousemove="moveTip($event)"
                @mouseleave="hideTip"
              >
                <circle
                  class="hit-area"
                  :cx="xOfMinutes(minutesFromMidnight(p.timeLabel))"
                  :cy="yOf(p.recordedCount, countAxisMax)"
                  r="10"
                />
                <circle
                  class="dot count"
                  :cx="xOfMinutes(minutesFromMidnight(p.timeLabel))"
                  :cy="yOf(p.recordedCount, countAxisMax)"
                  r="3.5"
                />
              </g>
            </svg>
            <div
              v-if="tip?.kind === 'count'"
              class="chart-tip"
              :style="{ left: `${tip.x + 12}px`, top: `${tip.y - 12}px` }"
            >
              <div class="tip-time">{{ selectedDate }} {{ tip.timeLabel }}</div>
              <div class="tip-row">在场人数 <strong>{{ tip.count }}</strong> 人</div>
              <div class="tip-row">占用率 <strong>{{ tip.rate }}%</strong></div>
            </div>
          </div>
        </section>

        <section class="chart-card">
          <div class="chart-head">
            <h3>占用率</h3>
            <span class="meta">
              当日峰值 {{ peakRate.toFixed(1) }}% · 纵轴 0–{{ rateAxisMax }}%（自适应）
            </span>
          </div>
          <div class="chart-stage">
            <svg class="chart" :viewBox="`0 0 ${W} ${H}`" role="img" aria-label="占用率波形">
              <defs>
                <linearGradient id="rateFill" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stop-color="#0a8a4a" stop-opacity="0.26" />
                  <stop offset="100%" stop-color="#0a8a4a" stop-opacity="0.02" />
                </linearGradient>
              </defs>

              <line
                v-for="t in rateYTicks"
                :key="'hg-r-' + t.label"
                class="grid-h"
                :x1="PAD.left"
                :x2="W - PAD.right"
                :y1="t.y"
                :y2="t.y"
              />
              <line
                v-for="t in hourTicks"
                :key="'gr-' + t.label"
                class="grid-v"
                :x1="t.x"
                :x2="t.x"
                :y1="PAD.top"
                :y2="H - PAD.bottom"
              />

              <line class="axis" :x1="PAD.left" :y1="H - PAD.bottom" :x2="W - PAD.right" :y2="H - PAD.bottom" />
              <line class="axis" :x1="PAD.left" :y1="PAD.top" :x2="PAD.left" :y2="H - PAD.bottom" />

              <text
                v-for="t in rateYTicks"
                :key="'ry-' + t.label"
                class="tick"
                :x="PAD.left - 8"
                :y="t.y + 4"
                text-anchor="end"
              >
                {{ t.label }}
              </text>
              <text
                v-for="t in hourTicks"
                :key="'lr-' + t.label"
                class="tick"
                :x="t.x"
                :y="H - 12"
                text-anchor="middle"
              >
                {{ t.label }}
              </text>

              <path v-if="rateArea" class="area" :d="rateArea" fill="url(#rateFill)" />
              <polyline v-if="ratePolyline" class="wave rate" fill="none" :points="ratePolyline" />

              <g
                v-for="(p, i) in series?.points ?? []"
                :key="'r-' + i"
                class="dot-hit"
                @mouseenter="showTip('rate', p, $event)"
                @mousemove="moveTip($event)"
                @mouseleave="hideTip"
              >
                <circle
                  class="hit-area"
                  :cx="xOfMinutes(minutesFromMidnight(p.timeLabel))"
                  :cy="yOf(Number(p.occupancyRate) || 0, rateAxisMax)"
                  r="10"
                />
                <circle
                  class="dot rate"
                  :cx="xOfMinutes(minutesFromMidnight(p.timeLabel))"
                  :cy="yOf(Number(p.occupancyRate) || 0, rateAxisMax)"
                  r="3.5"
                />
              </g>
            </svg>
            <div
              v-if="tip?.kind === 'rate'"
              class="chart-tip"
              :style="{ left: `${tip.x + 12}px`, top: `${tip.y - 12}px` }"
            >
              <div class="tip-time">{{ selectedDate }} {{ tip.timeLabel }}</div>
              <div class="tip-row">占用率 <strong>{{ tip.rate }}%</strong></div>
              <div class="tip-row">在场人数 <strong>{{ tip.count }}</strong> 人</div>
            </div>
          </div>
        </section>
      </template>

      <div class="calendar-dock">
        <label>
          查看日期
          <input v-model="selectedDate" type="date" :max="todayStr()" />
        </label>
        <span class="slots">理论 {{ SLOT_COUNT }} 点/天 · 已采 {{ series?.points?.length ?? 0 }} 点</span>
      </div>
    </div>

    <section class="movement-card">
      <div class="chart-head">
        <h3>进出场记录</h3>
        <span class="meta">签到/签退各记一条 · 共 {{ movements.length }} 条</span>
      </div>
      <div class="movement-scroll">
        <p v-if="loading" class="muted">加载中...</p>
        <p v-else-if="!movements.length" class="muted">该日暂无进出场记录</p>
        <table v-else>
          <thead>
            <tr>
              <th>会员ID</th>
              <th>时间</th>
              <th>类型</th>
              <th>场馆人数</th>
              <th>占用率</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="m in movements" :key="m.movementId">
              <td>#{{ m.memberId }}</td>
              <td>{{ fmtTime(m.eventTime) }}</td>
              <td>
                <span class="tag" :class="m.eventType === '1' ? 'out' : 'in'">
                  {{ m.eventTypeLabel }}
                </span>
              </td>
              <td>{{ m.recordedCount }}</td>
              <td>{{ m.occupancyRate }}%</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<style scoped>
.logs-page {
  display: grid;
  gap: 16px;
}

.charts-wrap {
  position: relative;
  display: grid;
  gap: 16px;
  padding: 20px 20px 72px;
  border-radius: 26px;
  background: #eef4fc;
  box-shadow: var(--tj-member-lift);
  border: var(--tj-member-edge);
}

.chart-card {
  display: grid;
  gap: 8px;
}

.chart-stage {
  position: relative;
}

.chart-head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}

.chart-head h3 {
  margin: 0;
  font-size: 16px;
  color: #1a2b45;
}

.meta {
  font-size: 13px;
  color: #7a88a0;
}

.chart {
  width: 100%;
  min-height: 320px;
  height: auto;
  display: block;
  background: linear-gradient(180deg, #fbfcfe 0%, #f4f7fb 100%);
  border-radius: 10px;
}

.grid-h {
  stroke: #e6edf5;
  stroke-width: 1;
}

.grid-v {
  stroke: #eef2f7;
  stroke-width: 1;
}

.axis {
  stroke: #c5d0e0;
  stroke-width: 1.2;
}

.tick {
  fill: #8a97ab;
  font-size: 11px;
}

.area {
  pointer-events: none;
}

.wave {
  stroke-width: 2.5;
  stroke-linejoin: round;
  stroke-linecap: round;
  pointer-events: none;
}

.wave.count {
  stroke: #1d4f91;
}

.wave.rate {
  stroke: #0a8a4a;
}

.hit-area {
  fill: transparent;
  cursor: pointer;
}

.dot.count {
  fill: #1d4f91;
  stroke: #fff;
  stroke-width: 1.2;
  pointer-events: none;
}

.dot.rate {
  fill: #0a8a4a;
  stroke: #fff;
  stroke-width: 1.2;
  pointer-events: none;
}

.dot-hit:hover .dot {
  r: 5;
}

.chart-tip {
  position: absolute;
  z-index: 5;
  min-width: 140px;
  padding: 8px 10px;
  border-radius: 8px;
  background: rgba(26, 43, 69, 0.92);
  color: #fff;
  font-size: 12px;
  line-height: 1.45;
  pointer-events: none;
  box-shadow: 0 6px 16px rgba(26, 43, 69, 0.22);
  transform: translateY(-100%);
}

.tip-time {
  margin-bottom: 4px;
  color: #b8c7db;
  font-size: 11px;
}

.tip-row strong {
  font-weight: 700;
  color: #fff;
}

.calendar-dock {
  position: absolute;
  right: 20px;
  bottom: 16px;
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 8px 12px;
  border-radius: 10px;
  background: #fff;
  border: 1px solid #d8e2f0;
  box-shadow: 0 4px 14px rgba(26, 43, 69, 0.08);
}

.calendar-dock label {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: #4a5a73;
}

.calendar-dock input {
  padding: 4px 8px;
  border: 1px solid #d8e2f0;
  border-radius: 6px;
  font-size: 13px;
}

.slots {
  font-size: 12px;
  color: #9aa6b8;
}

.muted {
  color: #999;
  text-align: center;
  padding: 12px 0;
}

.hint {
  margin: 0;
}

.error {
  color: #cf1322;
  text-align: center;
}

.movement-card {
  display: grid;
  gap: 10px;
  padding: 16px 20px 18px;
  border-radius: 24px;
  background: #eaf5f6;
  box-shadow: var(--tj-member-lift);
  border: var(--tj-member-edge);
}

.movement-scroll {
  max-height: 280px;
  overflow-y: auto;
  border: 1px solid #e8eef6;
  border-radius: 10px;
}

.movement-scroll table {
  width: 100%;
  border-collapse: collapse;
}

.movement-scroll th {
  position: sticky;
  top: 0;
  z-index: 1;
  text-align: left;
  padding: 10px 12px;
  font-size: 13px;
  color: #7a88a0;
  background: #f7fafc;
  border-bottom: 1px solid #e8eef6;
}

.movement-scroll td {
  padding: 10px 12px;
  font-size: 14px;
  border-bottom: 1px solid #eef2f7;
  color: #1a2b45;
}

.movement-scroll tr:last-child td {
  border-bottom: none;
}

.tag {
  display: inline-block;
  min-width: 44px;
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 12px;
  text-align: center;
  font-weight: 600;
}

.tag.in {
  color: #2a4365;
  background: #e8f0fb;
}

.tag.out {
  color: #8a4b12;
  background: #fff1e6;
}
</style>
