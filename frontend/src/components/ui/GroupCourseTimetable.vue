<script setup lang="ts">
// 周课课表：左时间轴可压缩空档；半透明色块叠加后重叠处自然加深

import { computed } from 'vue'
import type { GroupCourse } from '../../api/groupCourses'

const props = defineProps<{
  courses: GroupCourse[]
  ownedTypeIds: number[]
}>()

const WEEKDAYS = [
  { key: 1, label: '周一' },
  { key: 2, label: '周二' },
  { key: 3, label: '周三' },
  { key: 4, label: '周四' },
  { key: 5, label: '周五' },
  { key: 6, label: '周六' },
  { key: 7, label: '周日' },
] as const

type Block = {
  id: string
  courseId: number
  courseName: string
  coachName: string
  typeName: string
  weekday: number
  startMin: number
  endMin: number
  owned: boolean
  capacityLabel: string
}

function toHm(value: string) {
  const match = String(value).match(/(\d{2}):(\d{2})/)
  return match ? `${match[1]}:${match[2]}` : ''
}

function toMinutes(value: string) {
  const hm = toHm(value)
  if (!hm) return null
  const [h, m] = hm.split(':').map(Number)
  return h! * 60 + m!
}

function jsDayToWeekday(jsDay: number) {
  return jsDay === 0 ? 7 : jsDay
}

function formatMin(min: number) {
  const h = Math.floor(min / 60)
  const m = min % 60
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`
}

/** 取每门课的周课模式（按 weekday+时段 去重） */
const blocks = computed(() => {
  const owned = new Set(props.ownedTypeIds)
  const list: Block[] = []
  const seen = new Set<string>()

  for (const course of props.courses) {
    const patterns = new Map<string, { weekday: number; startMin: number; endMin: number }>()
    for (const slot of course.timeSlots ?? []) {
      const d = new Date(slot.courseDate)
      if (Number.isNaN(d.getTime())) continue
      const startMin = toMinutes(slot.startTime)
      const endMin = toMinutes(slot.endTime)
      if (startMin == null || endMin == null || endMin <= startMin) continue
      const weekday = jsDayToWeekday(d.getDay())
      const key = `${weekday}-${startMin}-${endMin}`
      if (!patterns.has(key)) {
        patterns.set(key, { weekday, startMin, endMin })
      }
    }

    for (const pattern of patterns.values()) {
      const id = `${course.courseId}-${pattern.weekday}-${pattern.startMin}`
      if (seen.has(id)) continue
      seen.add(id)
      list.push({
        id,
        courseId: course.courseId,
        courseName: course.courseName,
        coachName: course.coachName,
        typeName: course.courseTypeName,
        weekday: pattern.weekday,
        startMin: pattern.startMin,
        endMin: pattern.endMin,
        owned: owned.has(course.typeId),
        capacityLabel: `${course.currentCapacity}/${course.maxCapacity}`,
      })
    }
  }

  return list
})

/** 时间轴：有课区间按比例拉高，空档收缩 */
const axis = computed(() => {
  if (blocks.value.length === 0) {
    return { start: 8 * 60, end: 20 * 60, height: 360, segments: [] as { from: number; to: number; px: number; occupied: boolean }[] }
  }

  const mins = blocks.value.flatMap((b) => [b.startMin, b.endMin])
  let start = Math.max(0, Math.min(...mins) - 30)
  let end = Math.min(24 * 60, Math.max(...mins) + 30)
  start = Math.floor(start / 30) * 30
  end = Math.ceil(end / 30) * 30

  const occupied = new Array(end - start).fill(false)
  for (const b of blocks.value) {
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
    const px = flag ? Math.max(span * 1.35, 48) : Math.max(span * 0.35, 10)
    segments.push({ from: start + i, to: start + j, px, occupied: flag })
    i = j
  }

  const height = segments.reduce((s, x) => s + x.px, 0)
  return { start, end, height, segments }
})

function topPx(min: number) {
  let y = 0
  for (const seg of axis.value.segments) {
    if (min <= seg.from) break
    if (min >= seg.to) {
      y += seg.px
      continue
    }
    const ratio = (min - seg.from) / (seg.to - seg.from)
    y += seg.px * ratio
    break
  }
  return y
}

function blockStyle(block: Block) {
  const top = topPx(block.startMin)
  const bottom = topPx(block.endMin)
  return {
    top: `${top}px`,
    height: `${Math.max(bottom - top, 36)}px`,
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

const dayColumns = computed(() =>
  WEEKDAYS.map((d) => ({
    ...d,
    items: blocks.value.filter((b) => b.weekday === d.key),
  })),
)
</script>

<template>
  <div class="timetable">
    <div class="legend">
      <span class="dot owned" />已购课包类型
      <span class="dot other" />未购类型
      <span class="hint">半透明叠加：时间重叠处会加深</span>
    </div>

    <div v-if="blocks.length === 0" class="empty">暂无团课排期，请联系员工配置上课时间。</div>

    <div v-else class="grid" :style="{ '--tt-height': `${axis.height}px` }">
      <div class="corner">时间</div>
      <div v-for="day in WEEKDAYS" :key="day.key" class="day-head">{{ day.label }}</div>

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
        :key="day.key"
        class="day-col"
        :style="{ height: `${axis.height}px` }"
      >
        <article
          v-for="block in day.items"
          :key="block.id"
          class="block"
          :class="{ owned: block.owned, other: !block.owned }"
          :style="blockStyle(block)"
          :title="`${block.courseName} · ${block.coachName} · ${formatMin(block.startMin)}-${formatMin(block.endMin)}`"
        >
          <strong>{{ block.courseName }}</strong>
          <span>{{ block.coachName }}</span>
          <span>{{ formatMin(block.startMin) }}-{{ formatMin(block.endMin) }}</span>
          <span>{{ block.capacityLabel }} · {{ block.owned ? '已购类型' : '未购' }}</span>
        </article>
      </div>
    </div>
  </div>
</template>

<style scoped>
.timetable {
  --owned: rgba(37, 99, 235, 0.48);
  --other: rgba(100, 116, 139, 0.38);
  --owned-border: rgba(37, 99, 235, 0.75);
  --other-border: rgba(100, 116, 139, 0.55);
}

.legend {
  display: flex;
  flex-wrap: wrap;
  gap: 14px;
  align-items: center;
  margin-bottom: 12px;
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

.dot.owned { background: var(--owned); box-shadow: inset 0 0 0 1px var(--owned-border); }
.dot.other { background: var(--other); box-shadow: inset 0 0 0 1px var(--other-border); }
.hint { color: #94a3b8; }

.empty {
  padding: 28px;
  text-align: center;
  color: #94a3b8;
  background: #f8fafc;
  border-radius: 12px;
}

.grid {
  display: grid;
  grid-template-columns: 64px repeat(7, minmax(88px, 1fr));
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

.corner { border-right: 1px solid #e2e8f0; }

.time-col,
.day-col {
  position: relative;
  border-right: 1px solid #eef2f7;
  background-image: linear-gradient(#f1f5f9 1px, transparent 1px);
  background-size: 100% 48px;
}

.time-col { border-right: 1px solid #e2e8f0; background: #fcfdff; }

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
  left: 4px;
  right: 4px;
  z-index: 1;
  padding: 6px 7px;
  border-radius: 8px;
  border: 1px solid transparent;
  color: #0f172a;
  font-size: 11px;
  line-height: 1.35;
  overflow: hidden;
  backdrop-filter: blur(2px);
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.block.owned {
  background: var(--owned);
  border-color: var(--owned-border);
}

.block.other {
  background: var(--other);
  border-color: var(--other-border);
}

.block strong {
  font-size: 12px;
  font-weight: 700;
}

.block span {
  opacity: 0.92;
}

@media (max-width: 900px) {
  .grid {
    grid-template-columns: 52px repeat(7, minmax(72px, 1fr));
    overflow-x: auto;
  }
}
</style>
