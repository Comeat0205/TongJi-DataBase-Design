<script setup lang="ts">
// 会员购买会员卡页面：下单后进入「我的订单」支付。

import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { getCardProducts, purchaseMembershipCard, type CardProduct } from '@/api/card-products'
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
const basePath = computed(() => '/member')
const displayName = computed(() => authStore.session?.displayName ?? '会员')

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

async function handlePurchase(product: CardProduct) {

  if (!memberId.value) {
    errorMessage.value = '请先登录会员账号后再购买。'
    return
  }

  const ok = window.confirm(
    `确认购买「${product.name}」，价格 ¥${product.price.toFixed(2)}？\n将生成待支付订单，可在「我的订单」中选用优惠券后支付。`,
  )
  if (!ok) {
    return
  }

  buyingPriceId.value = product.priceId
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const order = await purchaseMembershipCard({
      memberId: memberId.value,
      priceId: product.priceId,
    })

    const payable = order.payableAmount?.toFixed(2) ?? product.price.toFixed(2)
    successMessage.value = `已创建待支付订单（应付 ¥${payable}），即将跳转到「我的订单」。`
    setTimeout(() => {
      router.push(`${basePath.value}/orders`)
    }, 800)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '下单失败，请稍后重试。'
  } finally {
    buyingPriceId.value = null
  }
}

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
      :subtitle="`您可在此选购季卡、年卡或次卡。优惠券可在生成订单后选择使用。`"
    >
      <template #actions>
        <div class="header-actions">
          <RouterLink class="ghost-link" :to="`${basePath}/orders`">我的订单</RouterLink>
          <RouterLink class="ghost-link" :to="`${basePath}/cards`">我的会员卡</RouterLink>
        </div>
      </template>
    </PageHeader>

    <p v-if="successMessage" class="success-banner">{{ successMessage }}</p>

    <StateCard v-if="loading" message="商品列表加载中..." />
    <StateCard v-else-if="errorMessage && products.length === 0" :message="errorMessage" type="error" />

    <section v-else-if="products.length === 0" class="empty-panel">
      <h2>暂无可购商品</h2>
      <p>价格表 PRICE_LIST 里还没有 MEMBERSHIP_ 开头的会员卡商品，请联系管理员维护。</p>
    </section>

    <section v-else class="product-list panel-tone-blue">
      <p v-if="errorMessage" class="inline-error">{{ errorMessage }}</p>

      <article v-for="product in products" :key="product.priceId" class="product-item list-item">
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
          :disabled="buyingPriceId === product.priceId"
          @click="handlePurchase(product)"
        >
          {{ buyingPriceId === product.priceId ? '下单中...' : '立即购买' }}
        </button>
      </article>
    </section>
  </div>
</template>

<style scoped>
.card-product-page {
  max-width: 960px;
}

.ghost-link {
  display: inline-flex;
  align-items: center;
  padding: 10px 18px;
  border-radius: 999px;
  border: 1px solid rgba(42, 67, 101, 0.18);
  color: #2a4365;
  text-decoration: none;
  font-weight: 600;
  background: #eaf5f6;
}

.header-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.success-banner {
  margin: 0 0 16px;
  padding: 12px 16px;
  border-radius: 20px;
  background: #e8f4f5;
  color: #2a4365;
}

.empty-panel {
  padding: 24px;
  border-radius: 28px;
  background: #eaf2fa;
  box-shadow: var(--tj-member-lift); border: var(--tj-member-edge);
}

.product-item {
  padding: 24px;
  border-radius: 28px;
  /* 背景色由父级 panel-tone-* 提供 */
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
  padding: 22px;
  border-radius: 28px;
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
  color: #2a4365;
}

.buy-btn {
  margin-top: 18px;
  width: 100%;
  padding: 12px 18px;
  border: none;
  border-radius: 999px;
  background: #2a4365;
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
