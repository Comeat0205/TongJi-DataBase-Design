<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { ApiError } from '@/api/http'
import { getMemberSchedules, type MemberScheduleItem } from '@/api/member-schedules'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const memberId = computed(() => authStore.session?.userId ?? 1)

const schedules = ref<MemberScheduleItem[]>([])
const loading = ref(true)
const errorMessage = ref('')
const weekIndex = ref(0)

const WEEKDAYS = [
  { key: 1, label: '周一' },
  { key: 2, label: '周二' },
  { key: 3, label: '周三' },
  { key: 4, label: '周四' },
  { key: 5, label: '周五' },
  { key: 6, label: '周六' },
  { key: 7, label: '周日' },
] as const

function toLocalDate(value: string) {
  return new Date(value.endsWith('Z') ? value.slice(0, -1) : value)
}

function startOfDay(d: Date) {
  const x = new Date(d)
  x.setHours(0, 0, 0, 0)
  return x
}

function startOfWeekMonday(date: Date) {
  const d = startOfDay(date)
  const day = d.getDay()
  const diff = day === 0 ? -6 : 1 - day
  d.setDate(d.getDate() + diff)
  return d
}

function addDays(date: Date, n: number) {
  const d = new Date(date)
  d.setDate(d.getDate() + n)
  return d
}

function dateKey(d: Date) {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function formatDateShort(d: Date) {
  return `${d.getMonth() + 1}/${d.getDate()}`
}

function formatMin(min: number) {
  const h = Math.floor(min / 60)
  const m = min % 60
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`
}

function minutesOf(value: string) {
  const d = toLocalDate(value)
  return d.getHours() * 60 + d.getMinutes()
}

function formatType(type: string) {
  if (type === 'G') return '团操'
  if (type === 'P') return '私教'
  return type
}

function formatStatus(status: string | null) {
  if (status === '待上课' || status === '0') return '待上课'
  if (status === '上课中') return '上课中'
  if (status === '已完成' || status === '1') return '已完成'
  if (status === '2' || status === '已取消') return '已取消'
  return status ?? '未知'
}

function courseTitle(item: MemberScheduleItem) {
  return item.courseName?.trim() || (item.scheduleType === 'P' ? '私教课' : '团操课')
}

function coachLabel(item: MemberScheduleItem) {
  if (item.coachName?.trim()) {
    return item.coachId ? `${item.coachName}（#${item.coachId}）` : item.coachName
  }
  if (item.coachId) {
    return `教练 #${item.coachId}`
  }
  return '教练待同步'
}

type WeekRange = { monday: Date; sunday: Date; label: string }

const weekRanges = computed((): WeekRange[] => {
  const todayMonday = startOfWeekMonday(new Date())
  let lastDate = todayMonday

  for (const item of schedules.value) {
    const d = startOfDay(toLocalDate(item.scheduleDate || item.scheduleStart))
    if (d > lastDate) lastDate = d
  }

  const lastMonday = startOfWeekMonday(lastDate)
  const weeks: WeekRange[] = []
  for (let m = new Date(todayMonday); m <= lastMonday; m = addDays(m, 7)) {
    const sunday = addDays(m, 6)
    weeks.push({
      monday: new Date(m),
      sunday,
      label: `${formatDateShort(m)} – ${formatDateShort(sunday)}`,
    })
  }

  if (weeks.length === 0) {
    const sunday = addDays(todayMonday, 6)
    weeks.push({
      monday: todayMonday,
      sunday,
      label: `${formatDateShort(todayMonday)} – ${formatDateShort(sunday)}`,
    })
  }

  return weeks
})

watch(weekRanges, (ranges) => {
  if (weekIndex.value >= ranges.length) {
    weekIndex.value = Math.max(0, ranges.length - 1)
  }
})

const currentWeek = computed(() => weekRanges.value[weekIndex.value] ?? weekRanges.value[0]!)
const canPrev = computed(() => weekIndex.value > 0)
const canNext = computed(() => weekIndex.value < weekRanges.value.length - 1)

function prevWeek() {
  if (canPrev.value) weekIndex.value -= 1
}

function nextWeek() {
  if (canNext.value) weekIndex.value += 1
}

type DayColumn = {
  key: number
  label: string
  date: Date
  dateKey: string
  isPast: boolean
  items: MemberScheduleItem[]
}

const dayColumns = computed((): DayColumn[] => {
  const monday = currentWeek.value.monday
  const today = startOfDay(new Date())

  return WEEKDAYS.map((wd, i) => {
    const date = addDays(monday, i)
    const key = dateKey(date)
    const items = schedules.value.filter((item) => {
      const d = startOfDay(toLocalDate(item.scheduleDate || item.scheduleStart))
      return dateKey(d) === key
    })
    return {
      key: wd.key,
      label: wd.label,
      date,
      dateKey: key,
      isPast: date < today,
      items,
    }
  })
})

const weekBlocks = computed(() =>
  dayColumns.value.flatMap((col) =>
    col.items.map((item) => ({
      item,
      startMin: minutesOf(item.scheduleStart),
      endMin: Math.max(minutesOf(item.scheduleEnd), minutesOf(item.scheduleStart) + 30),
    })),
  ),
)

const axis = computed(() => {
  const blocks = weekBlocks.value
  if (blocks.length === 0) {
    return {
      start: 8 * 60,
      end: 20 * 60,
      height: 360,
      segments: [] as { from: number; to: number; px: number; occupied: boolean }[],
    }
  }

  const mins = blocks.flatMap((b) => [b.startMin, b.endMin])
  let start = Math.max(0, Math.min(...mins) - 30)
  let end = Math.min(24 * 60, Math.max(...mins) + 30)
  start = Math.floor(start / 30) * 30
  end = Math.ceil(end / 30) * 30

  const occupied = new Array(end - start).fill(false)
  for (const b of blocks) {
    for (let m = Math.max(b.startMin, start); m < Math.min(b.endMin, end); m++) {
      occupied[m - start] = true
    }
  }

  const segments: { from: number; to: number; px: number; occupied: boolean }[] = []
  let i = 0
  while (i < occupied.length) {
    const flag = occupied[i]!
    let j = i + 1
    while (j < occupied.length && occupied[j] === flag) j++
    const span = j - i
    const px = flag ? Math.max(span * 1.35, 56) : Math.max(span * 0.35, 10)
    segments.push({ from: start + i, to: start + j, px, occupied: flag })
    i = j
  }

  return {
    start,
    end,
    height: segments.reduce((s, x) => s + x.px, 0),
    segments,
  }
})

function topPx(min: number) {
  let y = 0
  for (const seg of axis.value.segments) {
    if (min <= seg.from) break
    if (min >= seg.to) {
      y += seg.px
      continue
    }
    y += seg.px * ((min - seg.from) / (seg.to - seg.from))
    break
  }
  return y
}

function blockStyle(item: MemberScheduleItem) {
  const startMin = minutesOf(item.scheduleStart)
  const endMin = Math.max(minutesOf(item.scheduleEnd), startMin + 30)
  const top = topPx(startMin)
  const bottom = topPx(endMin)
  return {
    top: `${top}px`,
    height: `${Math.max(bottom - top, 48)}px`,
  }
}

const timeLabels = computed(() => {
  const labels: { min: number; top: number }[] = []
  for (const seg of axis.value.segments) {
    labels.push({ min: seg.from, top: topPx(seg.from) })
  }
  labels.push({ min: axis.value.end, top: axis.value.height })
  return labels
})

const weekHasItems = computed(() => dayColumns.value.some((d) => d.items.length > 0))

async function loadSchedules() {
  loading.value = true
  errorMessage.value = ''

  try {
    schedules.value = await getMemberSchedules(memberId.value)
    weekIndex.value = 0
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '日程加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

onMounted(loadSchedules)
</script>

<template>
  <div class="member-schedule">
    <PageHeader
      eyebrow="Member Schedule"
      title="我的日程"
      subtitle="按周课表查看课程安排；可切换星期。已过日期整列置灰（含当天已上完的课）；空档时间轴自动收缩。"
    >
      <template #actions>
        <button type="button" class="refresh-btn" :disabled="loading" @click="loadSchedules">
          刷新
        </button>
      </template>
    </PageHeader>

    <StateCard v-if="loading" message="日程加载中..." />
    <StateCard v-else-if="errorMessage" :message="errorMessage" type="error" />

    <template v-else>
      <div class="week-nav">
        <button type="button" class="nav-arrow" :disabled="!canPrev" aria-label="上一周" @click="prevWeek">
          ‹
        </button>
        <div class="week-label">
          <strong>{{ currentWeek.label }}</strong>
          <span>第 {{ weekIndex + 1 }} / {{ weekRanges.length }} 周 · 自本周一起</span>
        </div>
        <button type="button" class="nav-arrow" :disabled="!canNext" aria-label="下一周" @click="nextWeek">
          ›
        </button>
      </div>

      <div class="legend">
        <span class="dot type-pt" />私教
        <span class="dot type-group" />团操
        <span class="hint">半透明叠加：时间重叠处会加深；灰色列为已过日期</span>
      </div>

      <p v-if="schedules.length === 0" class="empty-tip">暂无日程安排。</p>

      <div v-else class="timetable">
        <p v-if="!weekHasItems" class="week-empty">本周暂无安排，可切换到其他周查看。</p>

        <div class="grid" :style="{ '--tt-height': `${axis.height}px` }">
          <div class="corner">时间</div>
          <div
            v-for="day in dayColumns"
            :key="day.dateKey"
            class="day-head"
            :class="{ past: day.isPast }"
          >
            <span>{{ day.label }}</span>
            <small>{{ formatDateShort(day.date) }}</small>
          </div>

          <div class="time-col" :style="{ height: `${axis.height}px` }">
            <div
              v-for="(label, idx) in timeLabels"
              :key="`${label.min}-${idx}`"
              class="tick"
              :style="{ top: `${label.top}px` }"
            >
              {{ formatMin(label.min) }}
            </div>
          </div>

          <div
            v-for="day in dayColumns"
            :key="`col-${day.dateKey}`"
            class="day-col"
            :class="{ past: day.isPast }"
            :style="{ height: `${axis.height}px` }"
          >
            <article
              v-for="item in day.items"
              :key="item.scheduleId"
              class="block"
              :class="{
                pt: item.scheduleType === 'P',
                group: item.scheduleType !== 'P',
                upcoming: item.isUpcoming,
              }"
              :style="blockStyle(item)"
              :title="`${courseTitle(item)} · ${formatType(item.scheduleType)} · ${formatStatus(item.status)}`"
            >
              <div class="block-badges">
                <span class="badge type">{{ formatType(item.scheduleType) }}</span>
                <span v-if="item.isUpcoming" class="badge upcoming-b">即将开课</span>
                <span class="badge status">{{ formatStatus(item.status) }}</span>
              </div>
              <strong>{{ courseTitle(item) }}</strong>
              <span>教练：{{ coachLabel(item) }}</span>
              <span>
                {{ formatMin(minutesOf(item.scheduleStart)) }}-{{ formatMin(minutesOf(item.scheduleEnd)) }}
              </span>
              <span v-if="item.sourceRecordId">预约编号 #{{ item.sourceRecordId }}</span>
            </article>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.member-schedule {
  display: grid;
  gap: 16px;
}

.refresh-btn {
  border: 0;
  border-radius: 10px;
  padding: 10px 18px;
  background: #315fe8;
  color: white;
  cursor: pointer;
  font-weight: 600;
}

.refresh-btn:disabled {
  opacity: 0.55;
  cursor: wait;
}

.week-nav {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16px;
  padding: 12px 16px;
  border-radius: 14px;
  background: var(--tj-card-bg, #fff);
  box-shadow: var(--tj-shadow, 0 2px 10px rgba(20, 34, 57, 0.06));
}

.nav-arrow {
  width: 40px;
  height: 40px;
  border: 1px solid #d7e0ef;
  border-radius: 12px;
  background: #f8fafc;
  color: #1e293b;
  font-size: 26px;
  line-height: 1;
  cursor: pointer;
  font-weight: 600;
}

.nav-arrow:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.week-label {
  display: grid;
  gap: 2px;
  text-align: center;
  min-width: 160px;
}

.week-label strong {
  font-size: 18px;
  color: #182337;
}

.week-label span {
  font-size: 12px;
  color: #7a88a0;
}

.legend {
  display: flex;
  flex-wrap: wrap;
  gap: 14px;
  align-items: center;
  color: #64748b;
  font-size: 13px;
}

.dot {
  width: 12px;
  height: 12px;
  border-radius: 3px;
  display: inline-block;
  margin-right: 6px;
  vertical-align: -1px;
}

.dot.type-pt {
  background: rgba(210, 105, 30, 0.45);
  box-shadow: inset 0 0 0 1px rgba(210, 105, 30, 0.75);
}

.dot.type-group {
  background: rgba(44, 87, 210, 0.45);
  box-shadow: inset 0 0 0 1px rgba(44, 87, 210, 0.75);
}

.hint {
  color: #94a3b8;
}

.empty-tip,
.week-empty {
  margin: 0;
  padding: 20px;
  border-radius: 12px;
  background: #f8fafc;
  color: #7a88a0;
  text-align: center;
}

.timetable {
  --pt: rgba(210, 105, 30, 0.42);
  --pt-border: rgba(210, 105, 30, 0.78);
  --group: rgba(44, 87, 210, 0.42);
  --group-border: rgba(44, 87, 210, 0.78);
}

.grid {
  display: grid;
  grid-template-columns: 64px repeat(7, minmax(96px, 1fr));
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  overflow: hidden;
  background: #fff;
}

.corner,
.day-head {
  padding: 10px 6px;
  text-align: center;
  font-weight: 700;
  font-size: 13px;
  color: #334155;
  background: #f8fafc;
  border-bottom: 1px solid #e2e8f0;
  border-right: 1px solid #e2e8f0;
}

.day-head {
  display: grid;
  gap: 2px;
}

.day-head small {
  font-weight: 500;
  color: #94a3b8;
  font-size: 11px;
}

.day-head.past,
.day-col.past {
  background: #f1f5f9;
  color: #94a3b8;
}

.day-col.past {
  background-image: linear-gradient(#e2e8f0 1px, transparent 1px);
  background-color: #f1f5f9;
}

.day-col.past .block {
  filter: grayscale(0.55);
  opacity: 0.72;
  color: #64748b;
}

.time-col,
.day-col {
  position: relative;
  border-right: 1px solid #eef2f7;
  background-image: linear-gradient(#f1f5f9 1px, transparent 1px);
  background-size: 100% 48px;
}

.time-col {
  border-right: 1px solid #e2e8f0;
  background: #fcfdff;
}

.tick {
  position: absolute;
  left: 0;
  right: 0;
  transform: translateY(-50%);
  font-size: 11px;
  color: #94a3b8;
  text-align: center;
  pointer-events: none;
}

.block {
  position: absolute;
  left: 3px;
  right: 3px;
  z-index: 1;
  padding: 6px 7px;
  border-radius: 8px;
  border: 1px solid transparent;
  color: #0f172a;
  font-size: 11px;
  line-height: 1.3;
  overflow-x: hidden;
  overflow-y: auto;
  overscroll-behavior: contain;
  backdrop-filter: blur(2px);
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  gap: 2px;
  scrollbar-width: thin;
  scrollbar-color: rgba(15, 23, 42, 0.35) transparent;
}

.block::-webkit-scrollbar {
  width: 4px;
}

.block::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: rgba(15, 23, 42, 0.3);
}

.block.pt {
  background: var(--pt);
  border-color: var(--pt-border);
}

.block.group {
  background: var(--group);
  border-color: var(--group-border);
}

.block strong {
  font-size: 12px;
  font-weight: 700;
}

.block-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}

.badge {
  padding: 1px 6px;
  border-radius: 999px;
  font-size: 10px;
  font-weight: 700;
  background: rgba(255, 255, 255, 0.55);
}

.badge.upcoming-b {
  background: #fff3cd;
  color: #b3541e;
}

@media (max-width: 900px) {
  .grid {
    grid-template-columns: 52px repeat(7, minmax(80px, 1fr));
    overflow-x: auto;
  }
}
</style>
