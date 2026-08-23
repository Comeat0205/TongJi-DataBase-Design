<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import PageHeader from '@/components/ui/PageHeader.vue'
import PlaceholderPanel from '@/components/ui/PlaceholderPanel.vue'
import {
  getCrowdHint,
  getCrowdLabel,
  memberBirthdayBenefitMock,
  memberCourseRecommendationsMock,
  memberMembershipMock,
  memberUpcomingRemindersMock,
  memberVenueCapacityMock,
  type CrowdLevel,
} from '@/data/home-dashboard-mock'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const basePath = computed(() => (route.path.startsWith('/preview/member') ? '/preview/member' : '/member'))
const memberId = computed(() => authStore.session?.userId)
const displayName = computed(() => authStore.session?.displayName ?? '会员')

const venue = memberVenueCapacityMock
const membership = memberMembershipMock
const recommendations = memberCourseRecommendationsMock
const reminders = memberUpcomingRemindersMock
const birthdayBenefit = memberBirthdayBenefitMock

const crowdClass = computed(() => `crowd-${venue.crowdLevel}`)

function crowdBarClass(level: CrowdLevel) {
  return `bar-${level}`
}

function goProfile() {
  if (memberId.value) {
    router.push(`${basePath.value}/profile/${memberId.value}`)
  }
}
</script>

<template>
  <div class="member-home">
    <PageHeader
      eyebrow="Member Dashboard"
      :title="`${displayName}，欢迎回来`"
      subtitle="会员登录首页占位：汇总场馆拥挤度、会籍状态、团课推荐与上课提醒。联调后数据来自 VENUE、MEMBER_BENEFIT_CARD、GROUPCOURSE 等表及相关业务接口。"
    >
      <template #actions>
        <button v-if="memberId" type="button" class="ghost-btn" @click="goProfile">我的档案</button>
        <RouterLink class="primary-link" :to="`${basePath}/check-in`">入场签到</RouterLink>
      </template>
    </PageHeader>

    <p class="demo-banner">演示数据 · 功能点占位 · 后续由 E/F/H/J 等模块接入真实 API</p>

    <section class="dashboard-grid">
      <article class="dashboard-card capacity-card" :class="crowdClass">
        <div class="card-head">
          <div>
            <p class="card-eyebrow">场馆实时容量 · 功能点 {{ venue.featureRef }}</p>
            <h2>{{ venue.venueName }}</h2>
          </div>
          <span class="status-pill" :class="crowdClass">{{ getCrowdLabel(venue.crowdLevel) }}</span>
        </div>
        <div class="capacity-stats">
          <div>
            <strong class="stat-value">{{ venue.currentCount }}</strong>
            <span class="stat-label">当前在馆</span>
          </div>
          <div>
            <strong class="stat-value">{{ venue.maxCapacity }}</strong>
            <span class="stat-label">最大容量</span>
          </div>
          <div>
            <strong class="stat-value">{{ venue.occupancyRate.toFixed(1) }}%</strong>
            <span class="stat-label">占用率</span>
          </div>
        </div>
        <div class="capacity-bar-track">
          <div
            class="capacity-bar-fill"
            :class="crowdBarClass(venue.crowdLevel)"
            :style="{ width: `${Math.min(venue.occupancyRate, 100)}%` }"
          />
        </div>
        <p class="card-hint">{{ getCrowdHint(venue.crowdLevel) }}</p>
        <p class="feature-note">需求 §1.1.3 · 超过 90% 黄灯预警，100% 禁止新入场（员工端前台同步展示）</p>
      </article>

      <article class="dashboard-card membership-card">
        <p class="card-eyebrow">我的会籍 · 功能点 {{ membership.featureRef }}</p>
        <h2>{{ membership.cardName }}</h2>
        <dl class="info-list">
          <div>
            <dt>有效期至</dt>
            <dd>{{ membership.expireDate }}</dd>
          </div>
          <div>
            <dt>剩余天数</dt>
            <dd>{{ membership.daysToExpire }} 天</dd>
          </div>
          <div>
            <dt>续费优惠</dt>
            <dd>{{ membership.renewalDiscountLabel }}</dd>
          </div>
        </dl>
        <RouterLink class="text-link" :to="`${basePath}/cards`">查看会员卡详情 →</RouterLink>
      </article>
    </section>

    <section class="dashboard-grid">
      <article class="dashboard-card span-2">
        <div class="card-head">
          <div>
            <p class="card-eyebrow">为你推荐团课 · 功能点 #19</p>
            <h2>热门时段智能推荐</h2>
          </div>
          <RouterLink class="text-link" :to="`${basePath}/group-courses`">全部团课</RouterLink>
        </div>
        <div class="recommend-list">
          <article v-for="course in recommendations" :key="course.courseId" class="recommend-item">
            <div>
              <h3>{{ course.courseName }}</h3>
              <p class="meta">{{ course.courseType }} · {{ course.coachName }} · {{ course.scheduleLabel }}</p>
              <p class="reason">{{ course.reason }}</p>
              <p class="feature-note">关联功能点 {{ course.featureRef }}</p>
            </div>
            <div class="recommend-side">
              <span class="slots-pill">余 {{ course.remainingSlots }} 名额</span>
              <RouterLink class="mini-btn" :to="`${basePath}/group-courses`">预约</RouterLink>
            </div>
          </article>
        </div>
      </article>

      <article class="dashboard-card">
        <p class="card-eyebrow">上课提醒 · 功能点 #11</p>
        <h2>即将开始</h2>
        <ul class="reminder-list">
          <li v-for="item in reminders" :key="item.bookingId">
            <strong>{{ item.title }}</strong>
            <span>{{ item.startTime }} · {{ item.venueName }}</span>
            <small>约 {{ item.minutesUntilStart }} 分钟后开始 · {{ item.featureRef }}</small>
          </li>
        </ul>
        <RouterLink class="text-link" :to="`${basePath}/schedule`">查看完整日程 →</RouterLink>
      </article>
    </section>

    <section class="dashboard-grid">
      <article class="dashboard-card promo-card">
        <p class="card-eyebrow">生日福利 · 功能点 {{ birthdayBenefit.featureRef }}</p>
        <h2>会员关怀</h2>
        <p>{{ birthdayBenefit.message }}</p>
        <p class="feature-note">生日当天入场时弹窗发放体验券（与 CHECKINOUT / VOUCHER 联动，H 模块实现）</p>
        <RouterLink class="text-link" :to="`${basePath}/vouchers`">我的优惠券 →</RouterLink>
      </article>

      <article class="dashboard-card quick-card span-2">
        <p class="card-eyebrow">快捷入口</p>
        <h2>常用功能</h2>
        <div class="quick-grid">
          <RouterLink :to="`${basePath}/group-courses`">团课预约 (#4 #8)</RouterLink>
          <RouterLink :to="`${basePath}/my-group-bookings`">我的团课 (#9 #10)</RouterLink>
          <RouterLink :to="`${basePath}/pt-bookings`">私教预约 (#12)</RouterLink>
          <RouterLink :to="`${basePath}/cards`">续费 / 购卡 (#20)</RouterLink>
        </div>
      </article>
    </section>

    <PlaceholderPanel
      owner="B + E/F/H/J"
      features="#7 #11 #18 #19 #20"
      message="本页为需求/设计/功能点驱动的首页占位。E 接入 VENUE/CAPACITYLOG；F/H 接入推荐与候补；J 接入日程提醒；D/H 接入会籍与续费折扣。"
    />
  </div>
</template>

<style scoped>
.member-home {
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

.dashboard-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

.dashboard-card {
  padding: 22px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.span-2 {
  grid-column: span 2;
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
  color: var(--tj-text);
  font-size: 22px;
}

.status-pill {
  padding: 6px 12px;
  border-radius: 999px;
  font-size: 13px;
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

.capacity-stats {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
  margin-bottom: 14px;
}

.stat-value {
  display: block;
  font-size: 28px;
  color: var(--tj-text);
}

.stat-label {
  color: var(--tj-text-muted);
  font-size: 13px;
}

.capacity-bar-track {
  height: 10px;
  border-radius: 999px;
  background: #eef2f7;
  overflow: hidden;
}

.capacity-bar-fill {
  height: 100%;
  border-radius: 999px;
}

.card-hint,
.feature-note {
  margin: 12px 0 0;
  color: var(--tj-text-muted);
  font-size: 13px;
  line-height: 1.6;
}

.info-list {
  display: grid;
  gap: 10px;
  margin: 16px 0;
}

.info-list div {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid #eef2f7;
}

.info-list dt {
  color: var(--tj-text-muted);
}

.info-list dd {
  margin: 0;
  font-weight: 600;
  color: var(--tj-text);
}

.recommend-list {
  display: grid;
  gap: 12px;
}

.recommend-item {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding: 14px;
  border-radius: 14px;
  background: #f8fbff;
  border: 1px solid #e6edf8;
}

.recommend-item h3 {
  margin: 0 0 6px;
  font-size: 17px;
}

.meta,
.reason {
  margin: 0;
  color: var(--tj-text-muted);
  font-size: 14px;
  line-height: 1.6;
}

.recommend-side {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 10px;
}

.slots-pill {
  padding: 4px 10px;
  border-radius: 999px;
  background: var(--tj-primary-soft);
  color: #2c57d2;
  font-size: 12px;
  font-weight: 600;
}

.reminder-list {
  list-style: none;
  padding: 0;
  margin: 16px 0;
  display: grid;
  gap: 12px;
}

.reminder-list li {
  display: grid;
  gap: 4px;
  padding: 12px;
  border-radius: 12px;
  background: #f8fbff;
}

.reminder-list span,
.reminder-list small {
  color: var(--tj-text-muted);
  font-size: 13px;
}

.promo-card {
  background: linear-gradient(180deg, #ffffff 0%, #fff9f0 100%);
}

.quick-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
  margin-top: 14px;
}

.quick-grid a,
.text-link,
.primary-link,
.mini-btn {
  color: #285cff;
  font-weight: 600;
  text-decoration: none;
}

.mini-btn,
.ghost-btn,
.primary-link {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 8px 12px;
  border-radius: 10px;
}

.mini-btn,
.primary-link {
  background: #285cff;
  color: #fff;
}

.ghost-btn {
  border: 1px solid #d9e2f0;
  background: #fff;
  color: var(--tj-text);
  cursor: pointer;
}

@media (max-width: 960px) {
  .dashboard-grid,
  .quick-grid {
    grid-template-columns: 1fr;
  }

  .span-2 {
    grid-column: span 1;
  }

  .recommend-item {
    flex-direction: column;
  }

  .recommend-side {
    align-items: flex-start;
  }
}
</style>
