<script setup lang="ts">
// 会员购买会员卡页面，阶段 5。

import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { getCardProducts, purchaseMembershipCardMock, type CardProduct } from '@/api/card-products'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const products = ref<CardProduct[]>([])
const loading = ref(true)
const errorMessage = ref('')
const buyingPriceId = ref<number | null>(null)
const successMessage = ref('')

const memberId = computed(() => authStore.session?.userId)
const isPreview = computed(() => route.path.startsWith('/preview/member'))
const basePath = computed(() => (isPreview.value ? '/preview/member' : '/member'))
const displayName = computed(() => authStore.session?.displayName ?? '会员')

// 加载商品列表
async function loadProducts() {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    products.value = await getCardProducts()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '商品列表加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

// 点击购买按钮
async function handlePurchase(product: CardProduct) {
  if (isPreview.value) {
    errorMessage.value = '预览模式仅展示界面，请登录会员账号后再购买。'
    return
  }

  if (!memberId.value) {
    errorMessage.value = '请先登录会员账号后再购买。'
    return
  }

  const ok = window.confirm(`确认购买"${product.name}"，价格 ¥${product.price} ？\n（MVP 阶段为模拟支付，不会创建真实订单）`)
  if (!ok) {
    return
  }

  buyingPriceId.value = product.priceId
  errorMessage.value = ''
  successMessage.value = ''

  try {
    await purchaseMembershipCardMock({
      memberId: memberId.value,
      priceId: product.priceId,
    })

    successMessage.value = `购买成功！即将跳转到"我的会员卡"页。`
    setTimeout(() => {
      router.push(`${basePath.value}/cards`)
    }, 800)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '购买失败，请稍后重试。'
  } finally {
    buyingPriceId.value = null
  }
}

// 把 cardType 转成中文标签
function getCardTypeLabel(cardType: string) {
  if (cardType === '0') {
    return '次卡'
  }

  if (cardType === '1') {
    return '时效卡'
  }

  return '会员卡'
}

onMounted(() => {
  loadProducts()
})
</script>

<template>
  <div class="card-product-page">
    <PageHeader
      eyebrow="Buy Membership"
      title="购买会员卡"
      :subtitle="`${displayName} 可在此选择季卡、年卡或次卡。MVP 阶段点击购买将直接模拟支付并发卡。`"
    >
      <template #actions>
        <RouterLink class="ghost-link" :to="`${basePath}/cards`">我的会员卡</RouterLink>
      </template>
    </PageHeader>

    <p v-if="isPreview" class="preview-banner">预览模式：仅展示页面布局，不能购买。请从登录页用会员账号进入。</p>

    <p v-if="successMessage" class="success-banner">{{ successMessage }}</p>

    <StateCard v-if="loading" message="商品列表加载中..." />
    <StateCard v-else-if="errorMessage && products.length === 0" :message="errorMessage" type="error" />

    <section v-else-if="products.length === 0" class="empty-panel">
      <h2>暂无可购商品</h2>
      <p>价格表 PRICE_LIST 里还没有 MEMBERSHIP_ 开头的会员卡商品，请联系管理员维护。</p>
    </section>

    <section v-else class="product-list">
      <p v-if="errorMessage" class="inline-error">{{ errorMessage }}</p>

      <article v-for="product in products" :key="product.priceId" class="product-item">
        <div class="product-head">
          <div>
            <p class="product-eyebrow">{{ getCardTypeLabel(product.cardType) }}</p>
            <h2>{{ product.name }}</h2>
          </div>
          <strong class="price">¥{{ product.price.toFixed(2) }}</strong>
        </div>

        <p class="product-desc">{{ product.description || product.productType }}</p>

        <button
          type="button"
          class="buy-btn"
          :disabled="buyingPriceId === product.priceId || isPreview"
          @click="handlePurchase(product)"
        >
          {{ buyingPriceId === product.priceId ? '购买中...' : '立即购买（模拟支付）' }}
        </button>
      </article>
    </section>
  </div>
</template>

<style scoped>
.card-product-page {
  max-width: 960px;
}

.preview-banner {
  margin: 0 0 16px;
  padding: 12px 16px;
  border-radius: 12px;
  background: #fff7ed;
  color: #c2410c;
}

.ghost-link {
  display: inline-flex;
  align-items: center;
  padding: 10px 18px;
  border-radius: 999px;
  border: 1px solid #d7e0ef;
  color: #2a3c59;
  text-decoration: none;
  font-weight: 600;
}

.success-banner {
  margin: 0 0 16px;
  padding: 12px 16px;
  border-radius: 12px;
  background: #e8f7ee;
  color: #15803d;
}

.empty-panel,
.product-item {
  padding: 24px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.empty-panel h2 {
  margin: 0 0 12px;
}

.empty-panel p,
.product-desc {
  margin: 0;
  color: var(--tj-text-muted);
  line-height: 1.7;
}

.product-list {
  display: grid;
  gap: 20px;
}

.inline-error {
  margin: 0;
  color: var(--tj-danger);
}

.product-head {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: flex-start;
  margin-bottom: 12px;
}

.product-eyebrow {
  margin: 0;
  color: #72819a;
  font-size: 13px;
}

.product-head h2 {
  margin: 8px 0 0;
  font-size: 24px;
  color: #142239;
}

.price {
  font-size: 28px;
  color: #4d77ff;
}

.buy-btn {
  margin-top: 18px;
  width: 100%;
  padding: 12px 18px;
  border: none;
  border-radius: 999px;
  background: #4d77ff;
  color: #fff;
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
}

.buy-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}
</style>
