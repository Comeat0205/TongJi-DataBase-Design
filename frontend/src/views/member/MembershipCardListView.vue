<script setup lang="ts">
// 会员"我的会员卡"列表页，阶段 3 第一个可演示页面。

import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ApiError } from '@/api/http'
import { getMyCards, type MembershipCard } from '@/api/membership-cards'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const authStore = useAuthStore()

const cards = ref<MembershipCard[]>([])
const loading = ref(true)
const errorMessage = ref('')

// 和首页、档案页一样，从登录会话取会员编号
const memberId = computed(() => authStore.session?.userId)
const basePath = computed(() => (route.path.startsWith('/preview/member') ? '/preview/member' : '/member'))
const displayName = computed(() => authStore.session?.displayName ?? '会员')

// 把日期格式化成中文显示
function formatDate(value?: string) {
  if (!value) {
    return '未填写'
  }

  return new Date(value).toLocaleDateString('zh-CN')
}

// 根据后端 isValid 和 cardStatus 显示状态文字
function getStatusText(card: MembershipCard) {
  if (card.isValid) {
    return '有效'
  }

  if (card.cardStatus === '2') {
    return '停用'
  }

  if (card.cardStatus === '0') {
    return '无效'
  }

  // 状态是 1 但 isValid 为 false，一般是过期或次数用完
  if (card.cardType === '0') {
    return '次数已用完'
  }

  if (card.cardType === '1') {
    return '已过期'
  }

  return '不可用'
}

// 次卡显示剩余次数，时效卡显示到期日
function getBenefitText(card: MembershipCard) {
  if (card.cardType === '0') {
    const total = card.totalCounts ?? '-'
    const left = card.remainingCount ?? '-'
    return `剩余 ${left} / ${total} 次`
  }

  if (card.cardType === '1') {
    return `到期日：${formatDate(card.expireDate)}`
  }

  return '暂无权益信息'
}

// 加载当前登录会员的卡列表
async function loadCards() {
  if (!memberId.value) {
    loading.value = false
    errorMessage.value = '请先登录会员账号后再查看会员卡。'
    return
  }

  loading.value = true
  errorMessage.value = ''

  try {
    cards.value = await getMyCards(memberId.value)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '会员卡加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadCards()
})
</script>

<template>
  <div class="membership-card-page">
    <PageHeader
      eyebrow="Membership Cards"
      title="我的会员卡"
      :subtitle="`${displayName} 的会籍卡列表，数据来自后端 /api/membership-cards 接口。`"
    >
      <template #actions>
        <RouterLink class="primary-link" :to="`${basePath}/card-products`">购买会员卡</RouterLink>
      </template>
    </PageHeader>

    <StateCard v-if="loading" message="会员卡加载中..." />
    <StateCard v-else-if="errorMessage" :message="errorMessage" type="error" />

    <section v-else-if="cards.length === 0" class="empty-panel">
      <h2>还没有会员卡</h2>
      <p>您当前名下没有可用的会员卡，可以先去购买页选一张合适的卡。</p>
      <RouterLink class="primary-link" :to="`${basePath}/card-products`">去购买会员卡</RouterLink>
    </section>

    <section v-else class="card-list">
      <article v-for="card in cards" :key="card.cardId" class="card-item">
        <div class="card-head">
          <div>
            <p class="card-eyebrow">卡号 #{{ card.cardId }}</p>
            <h2>{{ card.cardTypeLabel }}</h2>
          </div>
          <span class="status-badge" :class="{ valid: card.isValid }">{{ getStatusText(card) }}</span>
        </div>

        <div class="card-body">
          <div class="info-row">
            <span>发卡日期</span>
            <strong>{{ formatDate(card.issueDate) }}</strong>
          </div>
          <div class="info-row">
            <span>权益信息</span>
            <strong>{{ getBenefitText(card) }}</strong>
          </div>
          <div class="info-row">
            <span>卡状态码</span>
            <strong>{{ card.cardStatus ?? '未知' }}</strong>
          </div>
        </div>
      </article>
    </section>
  </div>
</template>

<style scoped>
.membership-card-page {
  max-width: 960px;
}

.primary-link {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 10px 18px;
  border-radius: 999px;
  background: #4d77ff;
  color: #fff;
  text-decoration: none;
  font-weight: 600;
}

.empty-panel {
  padding: 32px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.empty-panel h2 {
  margin: 0 0 12px;
  color: #142239;
}

.empty-panel p {
  margin: 0 0 20px;
  color: var(--tj-text-muted);
  line-height: 1.7;
}

.card-list {
  display: grid;
  gap: 20px;
}

.card-item {
  padding: 24px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.card-head {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: flex-start;
  margin-bottom: 18px;
}

.card-eyebrow {
  margin: 0;
  color: #72819a;
  font-size: 13px;
}

.card-head h2 {
  margin: 8px 0 0;
  font-size: 24px;
  color: #142239;
}

.status-badge {
  padding: 8px 14px;
  border-radius: 999px;
  background: #f3f4f6;
  color: #6b7280;
  font-size: 13px;
  white-space: nowrap;
}

.status-badge.valid {
  background: #e8f7ee;
  color: #15803d;
}

.card-body {
  display: grid;
  gap: 12px;
}

.info-row {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding-top: 12px;
  border-top: 1px solid #eef2f7;
}

.info-row span {
  color: #72819a;
}

.info-row strong {
  color: #2a3c59;
  text-align: right;
}
</style>
