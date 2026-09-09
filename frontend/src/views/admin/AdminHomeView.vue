<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import PageHeader from '@/components/ui/PageHeader.vue'
import { getDashboardStats, triggerAutoCheckout, type DashboardStats, type VenueStatus } from '@/api/check-in-out'
import {
  getCrowdHint,
  getCrowdLabel,
  type CrowdLevel,
} from '@/data/home-dashboard-mock'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const authStore = useAuthStore()

const basePath = computed(() => '/admin')
const displayName = computed(() => authStore.session?.displayName ?? '员工')

const stats = ref<DashboardStats>({ todayCheckIns: 0, activeMembers: 0, venues: [] })
const loading = ref(false)
const refreshing = ref(false)
const autoCheckoutMsg = ref('')
let timer: ReturnType<typeof setInterval> | null = null
let refreshAnimationTimer: ReturnType<typeof setTimeout> | null = null
let refreshAnimationFrame: number | null = null

/** 实时拥挤度仅展示主训练馆 */
const mainVenueCrowding = computed(() =>
  stats.value.venues.filter((v) => (v.venueName ?? '').includes('主训练')),
)

onMounted(async () => {
  await refresh()
  timer = setInterval(refresh, 30000) // 每 30 秒刷新
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
  if (refreshAnimationTimer) clearTimeout(refreshAnimationTimer)
  if (refreshAnimationFrame !== null) cancelAnimationFrame(refreshAnimationFrame)
})

function handleRefreshClick() {
  if (refreshAnimationTimer) clearTimeout(refreshAnimationTimer)
  if (refreshAnimationFrame !== null) cancelAnimationFrame(refreshAnimationFrame)

  refreshing.value = false
  refreshAnimationFrame = requestAnimationFrame(() => {
    refreshAnimationFrame = null
    refreshing.value = true
    refreshAnimationTimer = setTimeout(() => {
      refreshing.value = false
      refreshAnimationTimer = null
    }, 1000)
  })

  void refresh()
}

async function refresh() {
  if (loading.value) return

  loading.value = true
  const start = Date.now()
  try {
    stats.value = await getDashboardStats()
  } catch {
    // 接口异常时保留上次数据
  } finally {
    const elapsed = Date.now() - start
    if (elapsed < 475) await new Promise(r => setTimeout(r, 475 - elapsed))
    loading.value = false
  }
}

async function doAutoCheckout() {
  autoCheckoutMsg.value = ''
  try {
    const res = await triggerAutoCheckout()
    autoCheckoutMsg.value = res.message
    await refresh()
  } catch {
    autoCheckoutMsg.value = '自动签退执行失败'
  }
}

function getWarningLevel(v: VenueStatus): CrowdLevel {
  if (v.capacityWarningLevel === 'full') return 'full'
  if (v.capacityWarningLevel === 'warning') return 'warning'
  return 'comfortable'
}

function crowdBarClass(level: CrowdLevel) {
  return `bar-${level}`
}
</script>

<template>
  <div class="admin-home">
    <PageHeader
      eyebrow="Staff Dashboard"
      :title="`${displayName}，运营工作台`"
      subtitle="实时监控场馆拥挤度、容量预警、今日入场统计，支持一键自动签退。"
    >
      <template #actions>
        <RouterLink class="primary-link" :to="`${basePath}/check-in-desk`">前台入场</RouterLink>
      </template>
    </PageHeader>

    <section class="summary-grid">
      <article class="summary-card">
        <span>今日入场</span>
        <strong>{{ stats.todayCheckIns }}</strong>
        <small>人次</small>
      </article>
      <article class="summary-card">
        <span>当前在场</span>
        <strong>{{ stats.activeMembers }}</strong>
        <small>人</small>
      </article>
      <article class="summary-card">
        <span>场馆数量</span>
        <strong>{{ stats.venues.filter((v) => v.venueStatus !== '已关闭').length }}</strong>
        <small>个</small>
      </article>
      <article class="summary-card refresh-card" :class="{ refreshing }" @click="handleRefreshClick">
        <span>数据刷新</span>
        <div class="refresh-icon-wrap">
          <svg class="refresh-icon" :class="{ spinning: refreshing }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <path d="M21 2v6h-6"/>
            <path d="M3 12a9 9 0 0 1 15-6.7L21 8"/>
            <path d="M3 22v-6h6"/>
            <path d="M21 12a9 9 0 0 1-15 6.7L3 16"/>
          </svg>
        </div>
        <small>点击刷新 · 自动 30s</small>
      </article>
    </section>

    <section class="dashboard-card panel-tone-blue">
      <div class="card-head">
        <div>
          <p class="card-eyebrow">场馆容量监控</p>
          <h2>实时拥挤度</h2>
        </div>
        <div class="card-actions">
          <button class="btn-outline" @click="doAutoCheckout">一键自动签退</button>
          <RouterLink class="text-link" :to="`${basePath}/capacity-logs`">容量日志 →</RouterLink>
        </div>
      </div>
      <p v-if="autoCheckoutMsg" class="auto-msg">{{ autoCheckoutMsg }}</p>
      <div class="venue-list">
        <p v-if="!mainVenueCrowding.length" class="card-hint">暂无主训练馆数据</p>
        <article v-for="venue in mainVenueCrowding" :key="venue.venueId" class="venue-item list-item" :class="`crowd-${getWarningLevel(venue)}`">
          <div class="venue-top">
            <h3>{{ venue.venueName }}</h3>
            <span class="status-pill" :class="`crowd-${getWarningLevel(venue)}`">{{ getCrowdLabel(getWarningLevel(venue)) }}</span>
          </div>
          <p class="venue-meta">{{ venue.currentCapacity }} / {{ venue.maxCapacity }} 人 · 占用率 {{ venue.occupancyRate.toFixed(1) }}%</p>
          <div class="capacity-bar-track">
            <div
              class="capacity-bar-fill"
              :class="crowdBarClass(getWarningLevel(venue))"
              :style="{ width: `${Math.min(venue.occupancyRate, 100)}%` }"
            />
          </div>
          <p class="card-hint">{{ getCrowdHint(getWarningLevel(venue)) }}</p>
        </article>
      </div>
    </section>

    <section class="dashboard-card quick-card">
      <p class="card-eyebrow">快捷操作</p>
      <h2>前台常用</h2>
      <div class="quick-grid">
        <RouterLink :to="`${basePath}/members`">会员管理</RouterLink>
        <RouterLink :to="`${basePath}/check-in-desk`">入场 / 退场</RouterLink>
        <RouterLink :to="`${basePath}/capacity-logs`">容量日志</RouterLink>
        <RouterLink :to="`${basePath}/at-risk-members`">流失预警</RouterLink>
      </div>
    </section>
  </div>
</template>

<style scoped>
.admin-home {
  display: grid;
  gap: 20px;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.summary-card {
  padding: 16px;
  border-radius: 24px;
  background: #eef4fc;
  box-shadow: var(--tj-member-lift);
  border: var(--tj-member-edge);
}

.summary-card:nth-child(2) {
  background: #eaf5f6;
}

.summary-card:nth-child(3) {
  background: #e8f1f9;
}

.summary-card:nth-child(4) {
  background: #eaf2fa;
}

.summary-card span,
.summary-card small {
  display: block;
  color: var(--tj-text-muted);
  font-size: 13px;
}

.summary-card strong {
  display: block;
  margin: 8px 0;
  font-size: 28px;
  color: var(--tj-text);
}

.spinning {
  animation: spin 1s linear 1;
  transform-origin: center center;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(180deg); }
}

.refresh-card {
  cursor: pointer;
  transition: background 0.2s;
}
.refresh-card:hover {
  background: #eaf2fa;
}
.refresh-card.refreshing {
  background: #eaf2fa;
}

.refresh-icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 8px 0;
}

.refresh-icon {
  width: 28px;
  height: 28px;
  color: #2a4365;
}

.dashboard-card {
  padding: 22px;
  border-radius: 26px;
}

.dashboard-card.panel-tone-blue,
.dashboard-card.panel-tone-green {
  /* 背景色由 panel-tone-* 提供 */
}

.dashboard-card:not(.panel-tone-blue):not(.panel-tone-green) {
  background: #eef4fc;
  box-shadow: var(--tj-member-lift);
  border: var(--tj-member-edge);
}

.quick-card {
  max-width: 420px;
}

.card-head {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
  margin-bottom: 16px;
}

.card-actions {
  display: flex;
  gap: 12px;
  align-items: center;
}

.card-eyebrow {
  margin: 0 0 6px;
  color: #2a4365;
  font-size: 12px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.dashboard-card h2 {
  margin: 0;
  font-size: 22px;
  color: var(--tj-text);
}

.venue-list {
  display: grid;
  gap: 12px;
}

.venue-item {
  padding: 14px;
  border-radius: 14px;
  /* 背景色由父级 panel-tone-* 提供 */
}

.venue-top {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: center;
  margin-bottom: 8px;
}

.venue-item h3 {
  margin: 0;
  font-size: 17px;
}

.venue-meta,
.card-hint {
  margin: 8px 0 0;
  color: var(--tj-text-muted);
  font-size: 13px;
  line-height: 1.6;
}

.status-pill {
  padding: 4px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
}

.crowd-comfortable .status-pill,
.bar-comfortable {
  background: #e8f7ef;
  color: #137333;
}

.crowd-warning .status-pill,
.bar-warning {
  background: #fff4d6;
  color: #b45309;
}

.crowd-full .status-pill,
.bar-full {
  background: #fde8ea;
  color: #b42318;
}

.capacity-bar-track {
  height: 8px;
  border-radius: 999px;
  background: #eef2f7;
  overflow: hidden;
  margin-top: 10px;
}

.capacity-bar-fill {
  height: 100%;
  border-radius: 999px;
}

.quick-grid {
  display: grid;
  gap: 10px;
  margin-top: 14px;
}

.quick-grid a,
.text-link,
.primary-link {
  color: #2a4365;
  font-weight: 600;
  text-decoration: none;
}

.primary-link {
  display: inline-flex;
  padding: 8px 12px;
  border-radius: 10px;
  background: #2a4365;
  color: #fff;
}

.btn-outline {
  padding: 6px 14px;
  border: 1px solid #2a4365;
  border-radius: 8px;
  background: #eaf2fa;
  color: #2a4365;
  font-weight: 600;
  font-size: 13px;
  cursor: pointer;
}

.btn-outline:hover {
  background: #e8f1f9;
}

.auto-msg {
  margin: 0 0 12px;
  padding: 8px 12px;
  border-radius: 8px;
  background: #e8f7ef;
  color: #137333;
  font-size: 13px;
}

@media (max-width: 960px) {
  .summary-grid {
    grid-template-columns: 1fr;
  }

  .quick-card {
    max-width: none;
  }

  .venue-top {
    flex-direction: column;
  }
}
</style>
