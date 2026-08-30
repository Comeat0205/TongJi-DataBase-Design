/** 首页占位数据：联调 API 前用于展示布局与功能点对应关系 */

export type CrowdLevel = 'comfortable' | 'warning' | 'full'

export interface VenueCapacitySnapshot {
  venueId: number
  venueName: string
  currentCount: number
  maxCapacity: number
  occupancyRate: number
  crowdLevel: CrowdLevel
  featureRef: string
}

export interface MembershipSnapshot {
  cardName: string
  expireDate: string
  daysToExpire: number
  remainingTimes?: number
  renewalDiscountLabel: string
  featureRef: string
}

export interface CourseRecommendation {
  courseId: number
  courseName: string
  courseType: string
  coachName: string
  scheduleLabel: string
  remainingSlots: number
  reason: string
  featureRef: string
}

export interface UpcomingReminder {
  bookingId: number
  title: string
  startTime: string
  venueName: string
  minutesUntilStart: number
  featureRef: string
}

export interface AtRiskMember {
  memberId: number
  memberName: string
  attendanceDropRate: number
  lastVisitDate: string
  suggestedAction: string
  recommendedCourse: string
  featureRef: string
}

export function getCrowdLabel(level: CrowdLevel) {
  switch (level) {
    case 'comfortable':
      return '舒适'
    case 'warning':
      return '较拥挤'
    case 'full':
      return '已满员'
  }
}

export function getCrowdHint(level: CrowdLevel) {
  switch (level) {
    case 'comfortable':
      return '当前人数适中，适合入场训练。'
    case 'warning':
      return '占用率已超过 90%，建议错峰入场或预约团课。'
    case 'full':
      return '场馆已达上限，请稍后再试或改约其他时段。'
  }
}

/** 会员首页 · 场馆容量 #7 */
export const memberVenueCapacityMock: VenueCapacitySnapshot[] = [
  {
    venueId: 1,
    venueName: '主训练馆',
    currentCount: 449,
    maxCapacity: 500,
    occupancyRate: 89.8,
    crowdLevel: 'comfortable',
    featureRef: '#7',
  },
  {
    venueId: 160001,
    venueName: '测试团课区',
    currentCount: 49,
    maxCapacity: 50,
    occupancyRate: 98,
    crowdLevel: 'warning',
    featureRef: '#7',
  },
]

/** 会员首页 · 会籍摘要 #5 #6 #20 */
export const memberMembershipMock: MembershipSnapshot = {
  cardName: '尊享年卡',
  expireDate: '2026-04-12',
  daysToExpire: 12,
  remainingTimes: undefined,
  renewalDiscountLabel: '到期前 7 天内续费享 9 折',
  featureRef: '#5 #20',
}

export const memberCountCardMock: MembershipSnapshot = {
  cardName: '20 次次卡',
  expireDate: '2026-08-30',
  daysToExpire: 132,
  remainingTimes: 6,
  renewalDiscountLabel: '次数不足时可联系前台加购',
  featureRef: '#6',
}

/** 会员首页 · 团课推荐 #19 */
export const memberCourseRecommendationsMock: CourseRecommendation[] = [
  {
    courseId: 101,
    courseName: '燃脂动感单车',
    courseType: '动感单车',
    coachName: '王教练',
    scheduleLabel: '周三 19:00 - 19:45',
    remainingSlots: 4,
    reason: '根据您历史预约，偏好「工作日晚间」时段',
    featureRef: '#19',
  },
  {
    courseId: 102,
    courseName: '核心瑜伽 Flow',
    courseType: '瑜伽',
    coachName: '陈教练',
    scheduleLabel: '周四 19:30 - 20:15',
    remainingSlots: 8,
    reason: '与您常上的瑜伽课同类，时段相近',
    featureRef: '#19',
  },
  {
    courseId: 103,
    courseName: '力量塑形团课',
    courseType: '力量区',
    coachName: '刘教练',
    scheduleLabel: '周六 10:00 - 10:50',
    remainingSlots: 2,
    reason: '热门时段，建议尽早预约或加入候补',
    featureRef: '#9 #19',
  },
]

/** 会员首页 · 上课提醒 #11 */
export const memberUpcomingRemindersMock: UpcomingReminder[] = [
  {
    bookingId: 9001,
    title: '燃脂动感单车',
    startTime: '今天 19:00',
    venueName: '团课教室 A',
    minutesUntilStart: 95,
    featureRef: '#11',
  },
  {
    bookingId: 9002,
    title: '私教 · 下肢力量',
    startTime: '明天 18:30',
    venueName: '私教区 3 号位',
    minutesUntilStart: 1620,
    featureRef: '#11 #12',
  },
]

/** 会员首页 · 生日福利 #18 */
export const memberBirthdayBenefitMock = {
  enabled: false,
  message: '生日当天入场可领取「好友体验券」（7 天有效）',
  featureRef: '#18',
}

/** 员工首页 · 多场馆容量 #7 */
export const adminVenueCapacityListMock: VenueCapacitySnapshot[] = [
  {
    venueId: 1,
    venueName: '主训练馆',
    currentCount: 452,
    maxCapacity: 500,
    occupancyRate: 90.4,
    crowdLevel: 'warning',
    featureRef: '#7',
  },
  {
    venueId: 2,
    venueName: '瑜伽 / 普拉提室',
    currentCount: 28,
    maxCapacity: 40,
    occupancyRate: 70,
    crowdLevel: 'comfortable',
    featureRef: '#7',
  },
  {
    venueId: 3,
    venueName: '动感单车厅',
    currentCount: 32,
    maxCapacity: 32,
    occupancyRate: 100,
    crowdLevel: 'full',
    featureRef: '#7',
  },
]

/** 员工首页 · 流失风险会员 #17 */
export const adminAtRiskMembersMock: AtRiskMember[] = [
  {
    memberId: 10023,
    memberName: '张女士',
    attendanceDropRate: 58,
    lastVisitDate: '2026-02-28',
    suggestedAction: '销售回访 · 推送唤醒优惠券',
    recommendedCourse: '推荐：周三晚间瑜伽入门团课',
    featureRef: '#17',
  },
  {
    memberId: 10087,
    memberName: '李先生',
    attendanceDropRate: 52,
    lastVisitDate: '2026-03-02',
    suggestedAction: '教练跟进 · 建议预约体验私教',
    recommendedCourse: '推荐：周末力量塑形团课',
    featureRef: '#17 #19',
  },
]

/** 员工首页 · 今日运营摘要 */
export const adminOpsSummaryMock = {
  todayCheckIns: 186,
  pendingRepairs: 2,
  inspectionTasksDue: 5,
  hotWaitlistCourses: 1,
  featureRefs: '#5 #6 #15 #16 #9',
}

export interface CoachSessionItem {
  sessionId: number
  title: string
  sessionType: 'group' | 'pt'
  startTime: string
  endTime: string
  venueName: string
  enrolledCount?: number
  maxCapacity?: number
  memberName?: string
  status: 'upcoming' | 'in-progress' | 'completed'
  featureRef: string
}

export interface PendingPtConfirmation {
  bookingId: number
  memberName: string
  packageName: string
  scheduledAt: string
  venueName: string
  remainingSessions: number
  featureRef: string
}

export interface ScheduleConflictHint {
  conflictId: number
  message: string
  relatedSessions: string
  featureRef: string
}

/** 教练首页 · 今日授课 #4 #11 */
export const coachTodaySessionsMock: CoachSessionItem[] = [
  {
    sessionId: 501,
    title: '燃脂动感单车',
    sessionType: 'group',
    startTime: '09:00',
    endTime: '09:45',
    venueName: '动感单车厅',
    enrolledCount: 28,
    maxCapacity: 32,
    status: 'completed',
    featureRef: '#4',
  },
  {
    sessionId: 502,
    title: '核心瑜伽 Flow',
    sessionType: 'group',
    startTime: '14:00',
    endTime: '14:45',
    venueName: '瑜伽室 B',
    enrolledCount: 18,
    maxCapacity: 20,
    status: 'in-progress',
    featureRef: '#4 #11',
  },
  {
    sessionId: 503,
    title: '私教 · 下肢力量',
    sessionType: 'pt',
    startTime: '18:30',
    endTime: '19:30',
    venueName: '私教区 3 号位',
    memberName: '李先生',
    status: 'upcoming',
    featureRef: '#12 #13',
  },
  {
    sessionId: 504,
    title: '力量塑形团课',
    sessionType: 'group',
    startTime: '19:30',
    endTime: '20:20',
    venueName: '团课教室 A',
    enrolledCount: 15,
    maxCapacity: 16,
    status: 'upcoming',
    featureRef: '#4 #11',
  },
]

/** 教练首页 · 待确认私教 #13 #14 */
export const coachPendingPtConfirmMock: PendingPtConfirmation[] = [
  {
    bookingId: 8801,
    memberName: '张女士',
    packageName: '10 次私教课包',
    scheduledAt: '明天 10:00',
    venueName: '私教区 1 号位',
    remainingSessions: 4,
    featureRef: '#13 #14',
  },
  {
    bookingId: 8802,
    memberName: '王先生',
    packageName: '20 次私教课包',
    scheduledAt: '后天 16:00',
    venueName: '私教区 2 号位',
    remainingSessions: 11,
    featureRef: '#13 #14',
  },
]

/** 教练首页 · 排课冲突提示 #13 */
export const coachScheduleConflictMock: ScheduleConflictHint = {
  conflictId: 301,
  message: '周六 10:00 私教预约与团课排期存在场地重叠',
  relatedSessions: '私教 · 核心训练 / 力量塑形团课',
  featureRef: '#13',
}

/** 教练首页 · 今日摘要 */
export const coachOpsSummaryMock = {
  todayGroupSessions: 3,
  todayPtSessions: 2,
  pendingConfirmations: 2,
  upcomingReminders: 1,
  featureRefs: '#4 #11 #13 #14',
}
