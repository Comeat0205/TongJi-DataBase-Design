<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import {
  getMemberPersonalPackages,
  getPersonalPackageProducts,
  purchasePersonalPackage,
  type PersonalPackage,
  type PersonalPackageProduct,
} from '@/api/personal-packages'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const packages = ref<PersonalPackage[]>([])
const products = ref<PersonalPackageProduct[]>([])
const loading = ref(true)
const productsLoading = ref(true)
const errorMessage = ref('')
const productsError = ref('')
const successMessage = ref('')
const buyingPriceId = ref<number | null>(null)

const isPreview = computed(() => route.path.startsWith('/preview/member'))
const basePath = computed(() => (isPreview.value ? '/preview/member' : '/member'))
const memberId = computed(() =>
  authStore.session?.userType === 'member' ? authStore.session.userId : null,
)
const usableCount = computed(() => packages.value.filter((item) => item.isUsable).length)

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('zh-CN')
}

function formatValidUntil(validDays: number) {
  const date = new Date()
  date.setDate(date.getDate() + validDays)
  return date.toLocaleDateString('zh-CN')
}

async function loadPackages() {
  loading.value = true
  errorMessage.value = ''

  try {
    const id = memberId.value ?? 1
    packages.value = await getMemberPersonalPackages(id)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '私教课包加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

async function loadProducts() {
  productsLoading.value = true
  productsError.value = ''

  try {
    products.value = await getPersonalPackageProducts()
  } catch (error) {
    productsError.value =
      error instanceof ApiError ? error.message : '可购买课包加载失败，请稍后重试。'
  } finally {
    productsLoading.value = false
  }
}

async function refreshAll() {
  successMessage.value = ''
  await Promise.all([loadPackages(), loadProducts()])
}

async function handlePurchase(product: PersonalPackageProduct) {
  if (isPreview.value) {
    productsError.value = '预览模式仅展示界面，请登录会员账号后再购买。'
    return
  }

  if (!memberId.value) {
    productsError.value = '请先登录会员账号后再购买。'
    return
  }

  const ok = window.confirm(
    `确认购买「${product.courseName}」，价格 ¥${product.price.toFixed(2)}？\n将生成待支付订单，可在「我的订单」中选用优惠券后支付。`,
  )
  if (!ok) {
    return
  }

  buyingPriceId.value = product.priceId
  productsError.value = ''
  successMessage.value = ''

  try {
    const order = await purchasePersonalPackage({
      memberId: memberId.value,
      priceId: product.priceId,
    })

    const payable = order.payableAmount?.toFixed(2) ?? product.price.toFixed(2)
    successMessage.value = `已创建待支付订单（应付 ¥${payable}），即将跳转到「我的订单」。`
    setTimeout(() => {
      router.push(`${basePath.value}/orders`)
    }, 800)
  } catch (error) {
    productsError.value = error instanceof ApiError ? error.message : '下单失败，请稍后重试。'
  } finally {
    buyingPriceId.value = null
  }
}

onMounted(refreshAll)
</script>

<template>
  <div>
    <PageHeader
      eyebrow="Personal Training"
      title="我的私教课包"
      subtitle="查看已购买课包，也可挑选在售课包下单；支付成功后自动发放到本页。"
    >
      <template #actions>
        <div class="header-actions">
          <RouterLink class="ghost-link" :to="`${basePath}/orders`">我的订单</RouterLink>
          <button type="button" class="refresh-btn" :disabled="loading || productsLoading" @click="refreshAll">
            刷新
          </button>
        </div>
      </template>
    </PageHeader>

    <p v-if="isPreview" class="preview-banner">预览模式：仅展示页面布局，不能购买。请从登录页用会员账号进入。</p>
    <p v-if="successMessage" class="success-banner">{{ successMessage }}</p>

    <StateCard v-if="loading" message="私教课包加载中..." />
    <StateCard v-else-if="errorMessage" :message="errorMessage" type="error" />

    <template v-else>
      <section class="summary-strip">
        <div>
          <span>课包总数</span>
          <strong>{{ packages.length }}</strong>
        </div>
        <div>
          <span>当前可预约</span>
          <strong>{{ usableCount }}</strong>
        </div>
      </section>

      <h3 class="section-title">我的私教课包</h3>
      <StateCard v-if="packages.length === 0" message="当前没有已购私教课包，可在下方挑选购买。" />

      <section v-else class="package-grid">
        <article v-for="item in packages" :key="item.packageId" class="package-card">
          <div class="card-head">
            <div>
              <p>课包 #{{ item.packageId }}</p>
              <h2>{{ item.courseName }}</h2>
            </div>
            <span class="status" :class="{ disabled: !item.isUsable }">
              {{ item.isUsable ? '可预约' : '不可用' }}
            </span>
          </div>

          <p class="description">{{ item.courseDescription || '暂无课程简介' }}</p>

          <dl>
            <div>
              <dt>负责教练</dt>
              <dd>{{ item.coachName }}</dd>
            </div>
            <div>
              <dt>剩余次数</dt>
              <dd class="sessions">{{ item.remainingSessions }} / {{ item.totalSessions }}</dd>
            </div>
            <div>
              <dt>有效期至</dt>
              <dd>{{ formatDate(item.expireDate) }}</dd>
            </div>
            <div>
              <dt>数据库状态</dt>
              <dd>{{ item.packageStatus }}</dd>
            </div>
          </dl>
        </article>
      </section>
    </template>

    <h3 class="section-title buy-section-title">可供挑选购买</h3>
    <p class="section-hint">选择课包后点击「立即购买」，将跳转到我的订单完成支付；支付成功后课包会出现在上方「我的私教课包」中。</p>

    <StateCard v-if="productsLoading" message="可购买课包加载中..." />
    <StateCard v-else-if="productsError && products.length === 0" :message="productsError" type="error" />
    <StateCard v-else-if="products.length === 0" message="暂无可购私教课包，请联系管理员维护价格表商品。" />

    <template v-else>
      <p v-if="productsError" class="inline-error">{{ productsError }}</p>

      <section class="package-grid">
        <article v-for="product in products" :key="product.priceId" class="package-card">
          <div class="card-head">
            <div>
              <p>商品 #{{ product.priceId }}</p>
              <h2>{{ product.courseName }}</h2>
            </div>
            <span class="status">在售</span>
          </div>

          <p class="description">{{ product.courseDescription || '暂无课程简介' }}</p>

          <dl>
            <div>
              <dt>负责教练</dt>
              <dd>{{ product.coachName }}</dd>
            </div>
            <div>
              <dt>总次数</dt>
              <dd class="sessions">{{ product.totalSessions }}</dd>
            </div>
            <div>
              <dt>有效期至</dt>
              <dd>{{ formatValidUntil(product.validDays) }}</dd>
            </div>
            <div>
              <dt>价格</dt>
              <dd class="price">¥{{ product.price.toFixed(2) }}</dd>
            </div>
          </dl>

          <button
            type="button"
            class="buy-btn"
            :disabled="buyingPriceId === product.priceId || isPreview"
            @click="handlePurchase(product)"
          >
            {{ buyingPriceId === product.priceId ? '下单中...' : '立即购买' }}
          </button>
        </article>
      </section>
    </template>
  </div>
</template>

<style scoped>
.header-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
}

.ghost-link {
  display: inline-flex;
  align-items: center;
  padding: 10px 18px;
  border-radius: 10px;
  border: 1px solid #d7e0ef;
  color: #2a3c59;
  text-decoration: none;
  font-weight: 600;
}

.refresh-btn,
.buy-btn {
  border: 0;
  border-radius: 10px;
  padding: 10px 18px;
  background: #315fe8;
  color: white;
  cursor: pointer;
  font-weight: 600;
}

.refresh-btn:disabled,
.buy-btn:disabled {
  opacity: 0.55;
  cursor: wait;
}

.buy-btn {
  width: 100%;
  margin-top: 18px;
  padding: 12px 18px;
  border-radius: 999px;
}

.preview-banner,
.success-banner {
  margin: 0 0 16px;
  padding: 12px 16px;
  border-radius: 12px;
}

.preview-banner {
  background: #fff7ed;
  color: #c2410c;
}

.success-banner {
  background: #e8f7ee;
  color: #15803d;
}

.inline-error {
  margin: 0 0 12px;
  color: var(--tj-danger);
}

.summary-strip {
  display: flex;
  gap: 16px;
  margin-bottom: 20px;
}

.summary-strip div {
  min-width: 150px;
  padding: 16px 20px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.summary-strip span {
  display: block;
  color: var(--tj-text-muted);
  font-size: 13px;
}

.summary-strip strong {
  display: block;
  margin-top: 6px;
  color: var(--tj-text);
  font-size: 26px;
}

.section-title {
  margin: 8px 0 12px;
  color: var(--tj-text);
  font-size: 18px;
}

.buy-section-title {
  margin-top: 32px;
}

.section-hint {
  margin: 0 0 16px;
  color: var(--tj-text-muted);
  line-height: 1.6;
}

.package-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 18px;
}

.package-card {
  padding: 22px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.card-head {
  display: flex;
  justify-content: space-between;
  gap: 16px;
}

.card-head p,
.description {
  margin: 0;
  color: var(--tj-text-muted);
}

.card-head h2 {
  margin: 6px 0 0;
  color: var(--tj-text);
  font-size: 21px;
}

.status {
  height: fit-content;
  padding: 5px 10px;
  border-radius: 999px;
  background: #e9f8ef;
  color: #187342;
  font-size: 12px;
  white-space: nowrap;
}

.status.disabled {
  background: #f5eeee;
  color: #a13a3a;
}

.description {
  min-height: 42px;
  margin-top: 16px;
  line-height: 1.6;
}

dl {
  margin: 18px 0 0;
}

dl div {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding: 11px 0;
  border-top: 1px solid var(--tj-border);
}

dt {
  color: var(--tj-text-muted);
}

dd {
  margin: 0;
  color: var(--tj-text);
  font-weight: 600;
}

.sessions,
.price {
  color: #315fe8;
}

@media (max-width: 640px) {
  .summary-strip {
    display: grid;
    grid-template-columns: 1fr 1fr;
  }

  .summary-strip div {
    min-width: 0;
  }
}
</style>
