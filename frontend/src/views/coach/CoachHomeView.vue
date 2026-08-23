<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import PageHeader from '@/components/ui/PageHeader.vue'
import PlaceholderPanel from '@/components/ui/PlaceholderPanel.vue'
import {
  coachOpsSummaryMock,
  coachPendingPtConfirmMock,
  coachScheduleConflictMock,
  coachTodaySessionsMock,
  type CoachSessionItem,
} from '@/data/home-dashboard-mock'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const authStore = useAuthStore()

const basePath = computed(() => (route.path.startsWith('/preview/coach') ? '/preview/coach' : '/coach'))
const displayName = computed(() => authStore.session?.displayName ?? '教练')

const sessions = coachTodaySessionsMock
const pendingPt = coachPendingPtConfirmMock
const conflict = coachScheduleConflictMock
const ops = coachOpsSummaryMock

function sessionStatusLabel(status: CoachSessionItem['status']) {
  switch (status) {
    case 'completed':
      return '已结束'
    case 'in-progress':
      return '进行中'
    case 'upcoming':
      return '待开始'
  }
}

function sessionStatusClass(status: CoachSessionItem['status']) {
  return `status-${status}`
}
</script>

<template>
  <div class="coach-home">
    <PageHeader
      eyebrow="Coach Dashboard"
      :title="`${displayName}，教练工作台`"
      subtitle="教练登录首页占位：汇总今日授课、上课提醒、待确认私教与排课冲突。联调后数据来自 COACH、GROUPCOURSE、PTBOOKING 等接口。"
    >
      <template #actions>
        <RouterLink class="primary-link" :to="`${basePath}/pt-confirm`">私教确认</RouterLink>
      </template>
    </PageHeader>

    <p class="demo-banner">演示数据 · 功能点占位 · 后续由 G/J 等模块接入真实 API</p>

    <section class="summary-grid">
      <article class="summary-card">
        <span>今日团课</span>
        <strong>{{ ops.todayGroupSessions }}</strong>
        <small>功能点 #4</small>
      </article>
      <article class="summary-card">
        <span>今日私教</span>
        <strong>{{ ops.todayPtSessions }}</strong>
        <small>功能点 #12 #13</small>
      </article>
      <article class="summary-card highlight">
        <span>待确认私教</span>
        <strong>{{ ops.pendingConfirmations }}</strong>
        <small>功能点 #13 #14</small>
      </article>
      <article class="summary-card">
        <span>上课提醒</span>
        <strong>{{ ops.upcomingReminders }}</strong>
        <small>功能点 #11</small>
      </article>
    </section>

    <section v-if="conflict" class="conflict-banner">
      <div>
        <p class="conflict-eyebrow">排课冲突 · 功能点 {{ conflict.featureRef }}</p>
        <strong>{{ conflict.message }}</strong>
        <p class="conflict-meta">涉及：{{ conflict.relatedSessions }}</p>
      </div>
      <RouterLink class="text-link" :to="`${basePath}/schedule`">查看日程 →</RouterLink>
    </section>

    <section class="dashboard-grid">
      <article class="dashboard-card span-2">
        <div class="card-head">
          <div>
            <p class="card-eyebrow">今日授课 · 功能点 #4 #11</p>
            <h2>课程与私教安排</h2>
          </div>
          <RouterLink class="text-link" :to="`${basePath}/schedule`">完整日程 →</RouterLink>
        </div>
        <div class="session-list">
          <article v-for="session in sessions" :key="session.sessionId" class="session-item">
            <div class="session-time">
              <strong>{{ session.startTime }}</strong>
              <span>{{ session.endTime }}</span>
            </div>
            <div class="session-body">
              <div class="session-top">
                <h3>
                  {{ session.title }}
                  <small>{{ session.sessionType === 'group' ? '团课' : '私教' }}</small>
                </h3>
                <span class="status-pill" :class="sessionStatusClass(session.status)">
                  {{ sessionStatusLabel(session.status) }}
                </span>
              </div>
              <p class="meta">{{ session.venueName }}</p>
              <p v-if="session.sessionType === 'group'" class="meta">
                预约 {{ session.enrolledCount }} / {{ session.maxCapacity }} 人
              </p>
              <p v-else class="meta">会员：{{ session.memberName }}</p>
            </div>
            <span class="feature-tag">{{ session.featureRef }}</span>
          </article>
        </div>
      </article>

      <article class="dashboard-card">
        <div class="card-head">
          <div>
            <p class="card-eyebrow">待确认 · 功能点 #13 #14</p>
            <h2>私教预约确认</h2>
          </div>
        </div>
        <div class="pt-list">
          <article v-for="item in pendingPt" :key="item.bookingId" class="pt-item">
            <h3>{{ item.memberName }}</h3>
            <p class="meta">{{ item.packageName }} · 剩余 {{ item.remainingSessions }} 次</p>
            <p class="meta">{{ item.scheduledAt }} · {{ item.venueName }}</p>
            <span class="feature-tag">{{ item.featureRef }}</span>
          </article>
        </div>
        <RouterLink class="primary-link block-link" :to="`${basePath}/pt-confirm`">前往确认与消课</RouterLink>
      </article>
    </section>

    <PlaceholderPanel
      owner="G + J"
      features="#4 #11 #13 #14"
      message="教练首页占位：J 负责日程与冲突检测，G 负责私教确认与消课。完整能力在「我的日程」「私教确认」页面展开。"
    />
  </div>
</template>

<style scoped>
.coach-home {
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

.summary-card.highlight {
  border: 1px solid #ffd591;
  background: #fffaf0;
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

.conflict-banner {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: center;
  padding: 16px 18px;
  border-radius: var(--tj-radius);
  background: #fff1f0;
  border: 1px solid #ffccc7;
}

.conflict-eyebrow {
  margin: 0 0 4px;
  color: #cf1322;
  font-size: 12px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.conflict-meta {
  margin: 6px 0 0;
  color: var(--tj-text-muted);
  font-size: 13px;
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

.session-list,
.pt-list {
  display: grid;
  gap: 12px;
}

.session-item {
  display: grid;
  grid-template-columns: 72px 1fr auto;
  gap: 14px;
  align-items: start;
  padding: 14px;
  border-radius: 14px;
  background: #f8fbff;
  border: 1px solid #e6edf8;
}

.session-time {
  display: grid;
  gap: 2px;
  text-align: center;
}

.session-time strong {
  font-size: 18px;
  color: var(--tj-text);
}

.session-time span {
  font-size: 12px;
  color: var(--tj-text-muted);
}

.session-top {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: center;
}

.session-item h3,
.pt-item h3 {
  margin: 0;
  font-size: 17px;
}

.session-item h3 small {
  margin-left: 8px;
  color: var(--tj-text-muted);
  font-size: 12px;
  font-weight: 500;
}

.meta {
  margin: 6px 0 0;
  color: var(--tj-text-muted);
  font-size: 13px;
}

.status-pill {
  padding: 4px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
  white-space: nowrap;
}

.status-completed {
  background: #eef2f7;
  color: #5f6b7a;
}

.status-in-progress {
  background: #e8f7ef;
  color: #137333;
}

.status-upcoming {
  background: #eef3ff;
  color: #285cff;
}

.feature-tag {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 999px;
  background: #eef3fb;
  color: #4f5f7a;
  font-size: 12px;
  align-self: center;
}

.pt-item {
  padding: 14px;
  border-radius: 14px;
  background: #f8fbff;
  border: 1px solid #e6edf8;
}

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

.block-link {
  display: block;
  width: fit-content;
  margin-top: 14px;
}

@media (max-width: 960px) {
  .summary-grid,
  .dashboard-grid {
    grid-template-columns: 1fr;
  }

  .session-item {
    grid-template-columns: 1fr;
  }

  .conflict-banner {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
