<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import PageHeader from '@/components/ui/PageHeader.vue'
import PlaceholderPanel from '@/components/ui/PlaceholderPanel.vue'
import {
  adminAtRiskMembersMock,
  adminOpsSummaryMock,
  adminVenueCapacityListMock,
  getCrowdHint,
  getCrowdLabel,
  type CrowdLevel,
} from '@/data/home-dashboard-mock'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const authStore = useAuthStore()

const basePath = computed(() => (route.path.startsWith('/preview/admin') ? '/preview/admin' : '/admin'))
const displayName = computed(() => authStore.session?.displayName ?? '员工')

const venues = adminVenueCapacityListMock
const atRiskMembers = adminAtRiskMembersMock
const ops = adminOpsSummaryMock

function crowdBarClass(level: CrowdLevel) {
  return `bar-${level}`
}
</script>

<template>
  <div class="admin-home">
    <PageHeader
      eyebrow="Staff Dashboard"
      :title="`${displayName}，运营工作台`"
      subtitle="员工登录首页占位：监控场馆拥挤度、容量预警、流失风险会员与今日待办。联调后数据来自 VENUE、CAPACITYLOG、MEMBER 出勤统计等接口。"
    >
      <template #actions>
        <RouterLink class="primary-link" :to="`${basePath}/check-in-desk`">前台入场</RouterLink>
      </template>
    </PageHeader>

    <p class="demo-banner">演示数据 · 功能点占位 · 后续由 C/E/H/I 等模块接入真实 API</p>

    <section class="summary-grid">
      <article class="summary-card">
        <span>今日入场</span>
        <strong>{{ ops.todayCheckIns }}</strong>
        <small>{{ ops.featureRefs }}</small>
      </article>
      <article class="summary-card">
        <span>待处理报修</span>
        <strong>{{ ops.pendingRepairs }}</strong>
        <small>功能点 #15 · I</small>
      </article>
      <article class="summary-card">
        <span>待完成巡检</span>
        <strong>{{ ops.inspectionTasksDue }}</strong>
        <small>功能点 #16 · I</small>
      </article>
      <article class="summary-card">
        <span>候补中团课</span>
        <strong>{{ ops.hotWaitlistCourses }}</strong>
        <small>功能点 #9 · F</small>
      </article>
    </section>

    <section class="dashboard-card">
      <div class="card-head">
        <div>
          <p class="card-eyebrow">场馆容量监控 · 功能点 #7</p>
          <h2>实时拥挤度</h2>
        </div>
        <RouterLink class="text-link" :to="`${basePath}/capacity-logs`">容量日志 →</RouterLink>
      </div>
      <div class="venue-list">
        <article v-for="venue in venues" :key="venue.venueId" class="venue-item" :class="`crowd-${venue.crowdLevel}`">
          <div class="venue-top">
            <h3>{{ venue.venueName }}</h3>
            <span class="status-pill" :class="`crowd-${venue.crowdLevel}`">{{ getCrowdLabel(venue.crowdLevel) }}</span>
          </div>
          <p class="venue-meta">{{ venue.currentCount }} / {{ venue.maxCapacity }} 人 · 占用率 {{ venue.occupancyRate.toFixed(1) }}%</p>
          <div class="capacity-bar-track">
            <div
              class="capacity-bar-fill"
              :class="crowdBarClass(venue.crowdLevel)"
              :style="{ width: `${Math.min(venue.occupancyRate, 100)}%` }"
            />
          </div>
          <p class="card-hint">{{ getCrowdHint(venue.crowdLevel) }}</p>
        </article>
      </div>
      <p class="feature-note">需求 §1.1.3 · 设计文档 VENUE / CAPACITYLOG · 90% 黄灯、100% 禁入由 E 模块实现</p>
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
          <RouterLink :to="`${basePath}/members`">会员管理 (#1 #2)</RouterLink>
          <RouterLink :to="`${basePath}/check-in-desk`">入场 / 退场 (#5 #6)</RouterLink>
          <RouterLink :to="`${basePath}/repairs`">器材报修 (#15)</RouterLink>
          <RouterLink :to="`${basePath}/vouchers`">优惠券 (#18 #20)</RouterLink>
        </div>
      </article>
    </section>

    <PlaceholderPanel
      owner="C + E + H + I"
      features="#7 #9 #15 #16 #17"
      message="员工首页占位：E 负责容量与预警，H 负责流失会员与营销推荐，I 负责报修/巡检待办，C 负责会员/场馆基础信息维护入口。"
    />
  </div>
</template>

<style scoped>
.admin-home {
  display: grid;
  gap: 20px;
}

.demo-banner {
  margin: 0;
  padding: 10px 14px;
  border-radius: 12px;
  background: #fff7e6;
  color: #9a6700;
  font-size: 13px;
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
.card-hint,
.feature-note {
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
