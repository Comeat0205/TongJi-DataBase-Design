<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import PageHeader from '@/components/ui/PageHeader.vue'
import { getDashboardStats, triggerAutoCheckout, type DashboardStats, type VenueStatus } from '@/api/check-in-out'
import {
  adminAtRiskMembersMock,
  getCrowdHint,
  getCrowdLabel,
  type CrowdLevel,
} from '@/data/home-dashboard-mock'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const authStore = useAuthStore()

const basePath = computed(() => (route.path.startsWith('/preview/admin') ? '/preview/admin' : '/admin'))
const displayName = computed(() => authStore.session?.displayName ?? '员工')

const stats = ref<DashboardStats>({ todayCheckIns: 0, activeMembers: 0, venues: [] })
const atRiskMembers = adminAtRiskMembersMock
const loading = ref(false)
const autoCheckoutMsg = ref('')
let timer: ReturnType<typeof setInterval> | null = null

onMounted(async () => {
  await refresh()
  timer = setInterval(refresh, 30000) // 每 30 秒刷新
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
})

async function refresh() {
  loading.value = true
  try {
    stats.value = await getDashboardStats()
  } catch {
    // 接口异常时保留上次数据
  } finally {
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
        <strong>{{ stats.venues.length }}</strong>
        <small>个</small>
      </article>
      <article class="summary-card">
        <span>数据刷新</span>
        <strong :class="{ spinning: loading }">&#x21bb;</strong>
        <small>每 30 秒自动</small>
      </article>
    </section>

    <section class="dashboard-card">
      <div class="card-head">
        <div>
          <p class="card-eyebrow">场馆容量监控 · 功能点 #7</p>
          <h2>实时拥挤度</h2>
        </div>
        <div class="card-actions">
          <button class="btn-outline" @click="doAutoCheckout">一键自动签退</button>
          <RouterLink class="text-link" :to="`${basePath}/capacity-logs`">容量日志 →</RouterLink>
        </div>
      </div>
      <p v-if="autoCheckoutMsg" class="auto-msg">{{ autoCheckoutMsg }}</p>
      <div class="venue-list">
        <article v-for="venue in stats.venues" :key="venue.venueId" class="venue-item" :class="`crowd-${getWarningLevel(venue)}`">
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

    <section class="dashboard-grid">
      <article class="dashboard-card span-2">
        <div class="card-head">
          <div>
            <p class="card-eyebrow">流失风险 · 功能点 #17</p>
            <h2>待回访会员与课程推荐</h2>
          </div>
          <RouterLink class="text-link" :to="`${basePath}/at-risk-members`">全部名单 →</RouterLink>
        </div>
        <div class="risk-list">
          <article v-for="member in atRiskMembers" :key="member.memberId" class="risk-item">
            <div>
              <h3>{{ member.memberName }} <small>#{{ member.memberId }}</small></h3>
              <p class="meta">近 30 天出勤下降 {{ member.attendanceDropRate }}% · 上次到馆 {{ member.lastVisitDate }}</p>
              <p class="action">{{ member.suggestedAction }}</p>
            </div>
            <div class="risk-side">
              <p class="recommend">{{ member.recommendedCourse }}</p>
              <span class="feature-tag">{{ member.featureRef }}</span>
            </div>
          </article>
        </div>
      </article>

      <article class="dashboard-card">
        <p class="card-eyebrow">快捷操作</p>
        <h2>前台常用</h2>
        <div class="quick-grid">
          <RouterLink :to="`${basePath}/members`">会员管理</RouterLink>
          <RouterLink :to="`${basePath}/check-in-desk`">入场 / 退场</RouterLink>
          <RouterLink :to="`${basePath}/capacity-logs`">容量日志</RouterLink>
        </div>
      </article>
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
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
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
  display: inline-block;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.dashboard-grid {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 16px;
}

.dashboard-card {
  padding: 22px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.span-2 {
  grid-column: span 1;
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
  color: #4d77ff;
  font-size: 12px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.dashboard-card h2 {
  margin: 0;
  font-size: 22px;
  color: var(--tj-text);
}

.venue-list,
.risk-list {
  display: grid;
  gap: 12px;
}

.venue-item,
.risk-item {
  padding: 14px;
  border-radius: 14px;
  background: #f8fbff;
  border: 1px solid #e6edf8;
}

.venue-top,
.risk-item {
  display: flex;
  justify-content: space-between;
  gap: 16px;
}

.venue-top {
  align-items: center;
  margin-bottom: 8px;
}

.venue-item h3,
.risk-item h3 {
  margin: 0;
  font-size: 17px;
}

.venue-meta,
.meta,
.action,
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

.risk-side {
  min-width: 220px;
  text-align: right;
}

.recommend {
  margin: 0 0 8px;
  color: #285cff;
  font-size: 14px;
  font-weight: 600;
}

.feature-tag {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 999px;
  background: #eef3fb;
  color: #4f5f7a;
  font-size: 12px;
}

.quick-grid {
  display: grid;
  gap: 10px;
  margin-top: 14px;
}

.quick-grid a,
.text-link,
.primary-link {
  color: #285cff;
  font-weight: 600;
  text-decoration: none;
}

.primary-link {
  display: inline-flex;
  padding: 8px 12px;
  border-radius: 10px;
  background: #285cff;
  color: #fff;
}

.btn-outline {
  padding: 6px 14px;
  border: 1px solid #4d77ff;
  border-radius: 8px;
  background: #fff;
  color: #4d77ff;
  font-weight: 600;
  font-size: 13px;
  cursor: pointer;
}

.btn-outline:hover {
  background: #f0f5ff;
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
  .summary-grid,
  .dashboard-grid {
    grid-template-columns: 1fr;
  }

  .venue-top,
  .risk-item {
    flex-direction: column;
  }

  .risk-side {
    min-width: 0;
    text-align: left;
  }
}
</style>
