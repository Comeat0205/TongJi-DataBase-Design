<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import PageHeader from '@/components/ui/PageHeader.vue'
import PlaceholderPanel from '@/components/ui/PlaceholderPanel.vue'
import { checkOut, getMyCard, getMyCheckIn, getVenueStatus, type CheckInOutRecord, type MemberCard, type VenueStatus } from '@/api/check-in-out'
import {
  getCrowdHint,
  getCrowdLabel,
  memberBirthdayBenefitMock,
  memberCourseRecommendationsMock,
  memberUpcomingRemindersMock,
  type CrowdLevel,
} from '@/data/home-dashboard-mock'
import { useAuthStore } from '@/stores/auth'

interface VenueDisplay {
  venueId: number
  venueName: string
  currentCount: number
  maxCapacity: number
  occupancyRate: number
  crowdLevel: CrowdLevel
  featureRef: string
}

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const basePath = computed(() => (route.path.startsWith('/preview/member') ? '/preview/member' : '/member'))
const memberId = computed(() => authStore.session?.userId)
const displayName = computed(() => authStore.session?.displayName ?? '会员')

const venues = ref<VenueDisplay[]>([])
const recommendations = memberCourseRecommendationsMock
const reminders = memberUpcomingRemindersMock
const birthdayBenefit = memberBirthdayBenefitMock

const myCheckIn = ref<CheckInOutRecord | null>(null)
const myCard = ref<MemberCard | null>(null)
const checkoutLoading = ref(false)
const checkoutMsg = ref('')

function mapWarningLevel(level: string): CrowdLevel {
  if (level === 'full') return 'full'
  if (level === 'warning') return 'warning'
  return 'comfortable'
}

function toDisplay(v: VenueStatus): VenueDisplay {
  return {
    venueId: v.venueId,
    venueName: v.venueName,
    currentCount: v.currentCapacity,
    maxCapacity: v.maxCapacity,
    occupancyRate: v.occupancyRate,
    crowdLevel: mapWarningLevel(v.capacityWarningLevel),
    featureRef: '#7',
  }
}

function crowdClass(level: CrowdLevel) {
  return `crowd-${level}`
}

function crowdBarClass(level: CrowdLevel) {
  return `bar-${level}`
}

function goProfile() {
  if (memberId.value) {
    router.push(`${basePath.value}/profile/${memberId.value}`)
  }
}

async function refreshVenues() {
  try {
    const list = await getVenueStatus()
    venues.value = list.map(toDisplay)
  } catch { /* ignore */ }
}

onMounted(async () => {
  await refreshVenues()
  if (memberId.value) {
    try { myCheckIn.value = await getMyCheckIn(memberId.value) } catch { /* ignore */ }
    try { myCard.value = await getMyCard(memberId.value) } catch { /* ignore */ }
  }
})

async function doCheckOut() {
  if (!myCheckIn.value) return
  checkoutLoading.value = true
  checkoutMsg.value = ''
  try {
    await checkOut(myCheckIn.value.checkInOutId)
    myCheckIn.value = null
    checkoutMsg.value = '签退成功'
    await refreshVenues()
  } catch {
    checkoutMsg.value = '签退失败'
  } finally {
    checkoutLoading.value = false
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
        <template v-if="myCheckIn">
          <button class="checkout-btn" :disabled="checkoutLoading" @click="doCheckOut">
            {{ checkoutLoading ? '签退中...' : '签退' }}
          </button>
        </template>
        <template v-else>
          <RouterLink class="primary-link" :to="`${basePath}/check-in`">签到签退</RouterLink>
        </template>
      </template>
    </PageHeader>

    <p v-if="checkoutMsg" class="checkout-toast">{{ checkoutMsg }}</p>

    <p class="demo-banner">演示数据 · 功能点占位 · 后续由 E/F/H/J 等模块接入真实 API</p>

    <section class="top-grid">
      <div class="venues-column">
        <article v-for="v in venues" :key="v.venueId" class="dashboard-card capacity-card" :class="crowdClass(v.crowdLevel)">
          <div class="card-head">
            <div>
              <p class="card-eyebrow">场馆实时容量 · 功能点 {{ v.featureRef }}</p>
              <h2>{{ v.venueName }}</h2>
            </div>
            <span class="status-pill" :class="crowdClass(v.crowdLevel)">{{ getCrowdLabel(v.crowdLevel) }}</span>
          </div>
          <div class="capacity-stats">
            <div>
              <strong class="stat-value">{{ v.currentCount }}</strong>
              <span class="stat-label">当前在馆</span>
            </div>
            <div>
              <strong class="stat-value">{{ v.maxCapacity }}</strong>
              <span class="stat-label">最大容量</span>
            </div>
            <div>
              <strong class="stat-value">{{ v.occupancyRate.toFixed(1) }}%</strong>
              <span class="stat-label">占用率</span>
            </div>
          </div>
          <div class="capacity-bar-track">
            <div
              class="capacity-bar-fill"
              :class="crowdBarClass(v.crowdLevel)"
              :style="{ width: `${Math.min(v.occupancyRate, 100)}%` }"
            />
          </div>
          <p class="card-hint">{{ getCrowdHint(v.crowdLevel) }}</p>
        </article>
      </div>
      <div class="info-column">
        <article class="dashboard-card membership-card">
          <p class="card-eyebrow">我的会籍</p>
          <template v-if="myCard">
            <h2>{{ myCard.cardTypeName }} · {{ myCard.cardStatusName }}</h2>
            <dl class="info-list">
              <div v-if="myCard.expireDate">
                <dt>有效期至</dt>
                <dd>{{ myCard.expireDate }}</dd>
              </div>
              <div v-if="myCard.daysToExpire != null">
                <dt>剩余天数</dt>
                <dd :class="{ expired: myCard.daysToExpire < 0 }">{{ myCard.daysToExpire < 0 ? '已过期' : myCard.daysToExpire + ' 天' }}</dd>
              </div>
              <div v-if="myCard.remainingCount != null">
                <dt>剩余次数</dt>
                <dd>{{ myCard.remainingCount }} / {{ myCard.totalCounts }}</dd>
              </div>
            </dl>
          </template>
          <template v-else>
            <h2>暂无会员卡</h2>
            <p class="card-hint">请先购买会员卡</p>
          </template>
          <RouterLink class="text-link" :to="`${basePath}/cards`">查看会员卡详情 →</RouterLink>
        </article>
        <article class="dashboard-card promo-card">
          <div class="promo-top">
            <span class="promo-icon">🎁</span>
            <div>
              <p class="card-eyebrow">生日福利 · 功能点 {{ birthdayBenefit.featureRef }}</p>
              <h2>会员关怀</h2>
            </div>
          </div>
          <p class="promo-desc">{{ birthdayBenefit.message }}</p>
          <RouterLink class="promo-btn" :to="`${basePath}/vouchers`">查看我的优惠券 →</RouterLink>
        </article>
      </div>
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

.top-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
  align-items: stretch;
}

.venues-column {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.info-column {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.info-column .membership-card,
.info-column .promo-card {
  flex: 1;
}

.venues-column .capacity-card {
  flex: 1;
  padding: 18px 20px;
}

.venues-column .capacity-card .card-head {
  margin-bottom: 12px;
}

.venues-column .capacity-card h2 {
  font-size: 20px;
}

.venues-column .capacity-card .stat-value {
  font-size: 22px;
}

.venues-column .capacity-card .capacity-stats {
  margin-bottom: 10px;
}

.venues-column .capacity-card .card-hint {
  margin-top: 8px;
  font-size: 12px;
}

.venues-column .capacity-card .feature-note {
  display: none;
}

.membership-card {
  padding: 18px 20px;
}

.membership-card h2 {
  font-size: 17px;
  margin-top: 2px;
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
  margin: 14px 0;
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
  font-size: 14px;
}

.info-list dd {
  margin: 0;
  font-weight: 600;
  color: var(--tj-text);
  font-size: 15px;
}

.info-list dd.expired {
  color: #cf1322;
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
  padding: 18px 20px;
  background: linear-gradient(180deg, #ffffff 0%, #fff9f0 100%);
}

.promo-card h2 {
  font-size: 17px;
}

.promo-top {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.promo-icon {
  font-size: 28px;
  line-height: 1;
}

.promo-desc {
  color: var(--tj-text-muted);
  font-size: 14px;
  line-height: 1.6;
  margin: 0 0 14px;
}

.promo-btn {
  display: inline-flex;
  align-items: center;
  padding: 7px 14px;
  border-radius: 8px;
  background: #ff7a45;
  color: #fff;
  font-size: 13px;
  font-weight: 600;
  text-decoration: none;
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

.checkout-btn {
  padding: 8px 16px; border: none; border-radius: 10px;
  background: #cf1322; color: #fff; font-size: 14px; font-weight: 600; cursor: pointer;
}
.checkout-btn:disabled { opacity: .6; cursor: not-allowed; }

.checkout-toast {
  margin: 0; padding: 10px 16px; border-radius: 10px;
  background: #e6fff4; color: #0a8a4a; font-size: 14px; font-weight: 600;
}

@media (max-width: 960px) {
  .dashboard-grid,
  .top-grid,
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
