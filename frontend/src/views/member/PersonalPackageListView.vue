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
const errorMessage = ref('')
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

async function loadAll() {
  loading.value = true
  errorMessage.value = ''
  try {
    const id = memberId.value
    const tasks: Promise<unknown>[] = [
      getPersonalPackageProducts().then((data) => {
        products.value = data
      }),
    ]
    if (id) {
      tasks.push(
        getMemberPersonalPackages(id).then((data) => {
          packages.value = data
        }),
      )
    } else {
      packages.value = []
    }
    await Promise.all(tasks)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '私教课包加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

async function handlePurchase(product: PersonalPackageProduct) {
  if (isPreview.value) {
    errorMessage.value = '预览模式仅展示界面，请登录会员账号后再购买。'
    return
  }

  if (!memberId.value) {
    errorMessage.value = '请先登录会员账号后再购买。'
    return
  }

  const ok = window.confirm(
    `确认购买「${product.courseName}」，价格 ¥${product.price.toFixed(2)}？\n将生成待支付订单，可在「我的订单」中选用优惠券后支付。`,
  )
  if (!ok) return

  buyingPriceId.value = product.priceId
  errorMessage.value = ''
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
    errorMessage.value = error instanceof ApiError ? error.message : '下单失败，请稍后重试。'
  } finally {
    buyingPriceId.value = null
  }
}

onMounted(loadAll)
</script>

<template>
  <div class="pt-package-page">
    <PageHeader
      eyebrow="Personal Training"
      title="私教课包"
      subtitle="上方购买在售课包；支付成功后出现在下方「我的私教课包」，可前往私教预约使用。"
    >
      <template #actions>
        <RouterLink class="ghost-link" :to="`${basePath}/pt-bookings`">私教预约</RouterLink>
        <RouterLink class="ghost-link" :to="`${basePath}/orders`">我的订单</RouterLink>
        <button type="button" class="refresh-btn" :disabled="loading" @click="loadAll">刷新</button>
      </template>
    </PageHeader>

    <p v-if="isPreview" class="preview-banner">预览模式：仅展示页面布局，不能购买。请从登录页用会员账号进入。</p>
    <p v-if="successMessage" class="success-banner">{{ successMessage }}</p>
    <p v-if="errorMessage && !loading" class="error-banner">{{ errorMessage }}</p>

    <StateCard v-if="loading" message="私教课包加载中..." />

    <template v-else>
      <section class="purchase-section">
        <div class="section-head">
          <h2>购买私教课包</h2>
          <p>选择课包下单后到「我的订单」支付；支付成功后自动发放到本页下方。</p>
        </div>

        <StateCard v-if="products.length === 0" message="暂无可购私教课包，请联系管理员维护价格表商品。" />

        <div v-else class="product-grid">
          <article v-for="product in products" :key="product.priceId" class="product-card">
            <h3>{{ product.courseName }}</h3>
            <p class="meta">
              {{ product.coachName }} · {{ product.totalSessions }} 次 · 约至 {{ formatValidUntil(product.validDays) }}
            </p>
            <p v-if="product.courseDescription" class="desc">{{ product.courseDescription }}</p>
            <strong class="price">¥{{ product.price.toFixed(2) }}</strong>
            <button
              type="button"
              class="buy-btn"
              :disabled="buyingPriceId === product.priceId || isPreview"
              @click="handlePurchase(product)"
            >
              {{ buyingPriceId === product.priceId ? '下单中...' : '购买课包' }}
            </button>
          </article>
        </div>
      </section>

      <section class="mine-section">
        <div class="section-head">
          <h2>我的私教课包</h2>
          <p>共 {{ packages.length }} 个 · 当前可预约 {{ usableCount }} 个</p>
        </div>

        <StateCard v-if="packages.length === 0" message="还没有私教课包，请先在上方购买。" />

        <div v-else class="product-grid">
          <article v-for="item in packages" :key="item.packageId" class="product-card mine-card">
            <div class="card-top">
              <h3>{{ item.courseName }}</h3>
              <span class="status" :class="{ disabled: !item.isUsable }">
                {{ item.isUsable ? '可预约' : '不可用' }}
              </span>
            </div>
            <p class="meta">课包 #{{ item.packageId }} · {{ item.coachName }}</p>
            <p class="desc">{{ item.courseDescription || '暂无课程简介' }}</p>
            <dl class="kv">
              <div>
                <dt>剩余次数</dt>
                <dd>{{ item.remainingSessions }} / {{ item.totalSessions }}</dd>
              </div>
              <div>
                <dt>有效期至</dt>
                <dd>{{ formatDate(item.expireDate) }}</dd>
              </div>
              <div>
                <dt>状态</dt>
                <dd>{{ item.packageStatus }}</dd>
              </div>
            </dl>
            <RouterLink
              v-if="item.isUsable"
              class="buy-btn link-btn"
              :to="`${basePath}/pt-bookings`"
            >
              去预约
            </RouterLink>
          </article>
        </div>
      </section>
    </template>
  </div>
</template>

<style scoped>
.pt-package-page {
  width: 100%;
}

.ghost-link {
  display: inline-flex;
  margin-left: 8px;
  padding: 10px 16px;
  border-radius: 999px;
  border: 1px solid #d7e0ef;
  color: #2a3c59;
  text-decoration: none;
  font-weight: 600;
}

.refresh-btn {
  margin-left: 8px;
  padding: 10px 16px;
  border: 0;
  border-radius: 999px;
  background: #315fe8;
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}

.refresh-btn:disabled {
  opacity: 0.55;
  cursor: wait;
}

.preview-banner,
.success-banner,
.error-banner {
  margin: 0 0 16px;
  padding: 12px 16px;
  border-radius: 12px;
  font-weight: 600;
}

.preview-banner {
  background: #fff7ed;
  color: #c2410c;
}

.success-banner {
  background: #e8f7ee;
  color: #15803d;
}

.error-banner {
  background: #fcebed;
  color: var(--tj-danger, #cf1322);
}

.section-head {
  margin: 8px 0 16px;
}

.section-head h2 {
  margin: 0 0 6px;
}

.section-head p {
  margin: 0;
  color: #72819a;
}

.purchase-section,
.mine-section {
  margin-bottom: 28px;
}

.product-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 16px;
}

.product-card {
  padding: 20px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.card-top {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
}

.card-top h3,
.product-card h3 {
  margin: 0;
  font-size: 18px;
}

.meta,
.desc {
  margin: 0;
  color: #72819a;
  line-height: 1.5;
  font-size: 14px;
}

.price {
  color: #4d77ff;
  font-size: 22px;
}

.status {
  height: fit-content;
  padding: 4px 10px;
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

.kv {
  margin: 4px 0 0;
  display: grid;
  gap: 8px;
}

.kv div {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  padding-top: 8px;
  border-top: 1px solid #eef2f7;
}

.kv dt {
  color: #72819a;
  font-size: 13px;
}

.kv dd {
  margin: 0;
  font-weight: 600;
  color: #182337;
}

.buy-btn,
.link-btn {
  margin-top: auto;
  padding: 10px 14px;
  border: none;
  border-radius: 999px;
  background: #4d77ff;
  color: #fff;
  font-weight: 600;
  cursor: pointer;
  text-align: center;
  text-decoration: none;
}

.buy-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
