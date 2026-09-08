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
const PAD = { top: 28, right: 20, bottom: 40, left: 52 }

/** 低段局部放大：0–50 人 / 0–10% 占用约一半以上纵轴高度 */
const COUNT_LOW_MAX = 50
const RATE_LOW_MAX = 10
const LOW_BAND_HEIGHT_RATIO = 0.58

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

function xOfMinutes(mins: number) {
  const inner = W - PAD.left - PAD.right
  return PAD.left + (mins / (24 * 60 - 10)) * inner
}

/**
 * 分段纵轴：0–lowMax 映射到底部 LOW_BAND_HEIGHT_RATIO 高度，其余映射到上方剩余高度。
 * 人数少时波形更明显；超过低段后仍能画到满轴。
 */
function yOf(value: number, max: number, lowMax: number) {
  const inner = H - PAD.top - PAD.bottom
  if (max <= 0) return PAD.top + inner
  const v = Math.min(Math.max(value, 0), max)
  const band = Math.min(lowMax, max)
  const lowH = inner * LOW_BAND_HEIGHT_RATIO
  const highH = inner - lowH

  let fromBottom: number
  if (v <= band || max <= band) {
    fromBottom = (v / band) * (max <= band ? inner : lowH)
  } else {
    fromBottom = lowH + ((v - band) / (max - band)) * highH
  }
  return PAD.top + inner - fromBottom
}

function buildPolyline(
  points: CapacityLogPoint[],
  valueOf: (p: CapacityLogPoint) => number,
  max: number,
  lowMax: number,
) {
  if (!points.length || max <= 0) return ''
  return points
    .map((p) => {
      const x = xOfMinutes(minutesFromMidnight(p.timeLabel))
      const y = yOf(valueOf(p), max, lowMax)
      return `${x.toFixed(1)},${y.toFixed(1)}`
    })
    .join(' ')
}

const countMax = computed(() => {
  const maxCap = series.value?.maxCapacity ?? 0
  const peak = series.value?.points.reduce((m, p) => Math.max(m, p.recordedCount), 0) ?? 0
  return Math.max(maxCap, peak, 1)
})

const countPolyline = computed(() =>
  buildPolyline(series.value?.points ?? [], (p) => p.recordedCount, countMax.value, COUNT_LOW_MAX),
)

const ratePolyline = computed(() =>
  buildPolyline(series.value?.points ?? [], (p) => p.occupancyRate, 100, RATE_LOW_MAX),
)

const countYTicks = computed(() => {
  const max = countMax.value
  const values = max <= COUNT_LOW_MAX
    ? [0, Math.round(max / 2), max]
    : [0, 25, COUNT_LOW_MAX, Math.round((COUNT_LOW_MAX + max) / 2), max]
  return [...new Set(values)].map((v) => ({
    label: String(v),
    y: yOf(v, max, COUNT_LOW_MAX),
  }))
})

const rateYTicks = computed(() => {
  const values = [0, 5, RATE_LOW_MAX, 50, 100]
  return values.map((v) => ({
    label: `${v}%`,
    y: yOf(v, 100, RATE_LOW_MAX),
  }))
})

const countBreakY = computed(() => yOf(COUNT_LOW_MAX, countMax.value, COUNT_LOW_MAX))
const rateBreakY = computed(() => yOf(RATE_LOW_MAX, 100, RATE_LOW_MAX))

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
  // 今日图：每分钟刷新一次，靠近整十分钟时能尽快看到新点
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

        <!-- 在场人数 -->
        <section class="chart-card">
          <div class="chart-head">
            <h3>实时在场人数</h3>
            <span class="meta">上限 {{ series?.maxCapacity ?? '-' }} · 0–50 人纵轴放大</span>
          </div>
          <div class="chart-stage">
            <svg class="chart" :viewBox="`0 0 ${W} ${H}`" role="img" aria-label="在场人数波形">
              <line
                v-for="t in hourTicks"
                :key="'gc-' + t.label"
                class="grid"
                :x1="t.x"
                :x2="t.x"
                :y1="PAD.top"
                :y2="H - PAD.bottom"
              />
              <line
                v-if="countMax > COUNT_LOW_MAX"
                class="band-break"
                :x1="PAD.left"
                :x2="W - PAD.right"
                :y1="countBreakY"
                :y2="countBreakY"
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
              <polyline
                v-if="countPolyline"
                class="wave count"
                fill="none"
                :points="countPolyline"
              />
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
                  :cy="yOf(p.recordedCount, countMax, COUNT_LOW_MAX)"
                  r="10"
                />
                <circle
                  class="dot count"
                  :cx="xOfMinutes(minutesFromMidnight(p.timeLabel))"
                  :cy="yOf(p.recordedCount, countMax, COUNT_LOW_MAX)"
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

        <!-- 占用率 -->
        <section class="chart-card">
          <div class="chart-head">
            <h3>占用率</h3>
            <span class="meta">0% – 100% · 0–10% 纵轴放大</span>
          </div>
          <div class="chart-stage">
            <svg class="chart" :viewBox="`0 0 ${W} ${H}`" role="img" aria-label="占用率波形">
              <line
                v-for="t in hourTicks"
                :key="'gr-' + t.label"
                class="grid"
                :x1="t.x"
                :x2="t.x"
                :y1="PAD.top"
                :y2="H - PAD.bottom"
              />
              <line
                class="band-break"
                :x1="PAD.left"
                :x2="W - PAD.right"
                :y1="rateBreakY"
                :y2="rateBreakY"
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
              <polyline
                v-if="ratePolyline"
                class="wave rate"
                fill="none"
                :points="ratePolyline"
              />
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
                  :cy="yOf(p.occupancyRate, 100, RATE_LOW_MAX)"
                  r="10"
                />
                <circle
                  class="dot rate"
                  :cx="xOfMinutes(minutesFromMidnight(p.timeLabel))"
                  :cy="yOf(p.occupancyRate, 100, RATE_LOW_MAX)"
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
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
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
  background: #f7fafc;
  border-radius: 10px;
}

.grid {
  stroke: #e8eef6;
  stroke-width: 1;
}

.band-break {
  stroke: #c5d0e0;
  stroke-width: 1.25;
  stroke-dasharray: 5 4;
}

.axis {
  stroke: #c5d0e0;
  stroke-width: 1.2;
}

.tick {
  fill: #8a97ab;
  font-size: 11px;
}

.wave {
  stroke-width: 2.4;
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
  pointer-events: none;
}

.dot.rate {
  fill: #0a8a4a;
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
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
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
  color: #1d4f91;
  background: #e8f0fb;
}

.tag.out {
  color: #8a4b12;
  background: #fff1e6;
}
</style>
