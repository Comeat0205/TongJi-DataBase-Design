<script setup lang="ts">
// 团课预约页：购买课包 + 周课课表（团课安排）

import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import PageHeader from '../../components/ui/PageHeader.vue'
import StateCard from '../../components/ui/StateCard.vue'
import GroupCourseTimetable from '../../components/ui/GroupCourseTimetable.vue'
import { getGroupCourses, type GroupCourse } from '../../api/groupCourses'
import {
  getGroupPackageProducts,
  getMyGroupPackages,
  purchaseGroupPackage,
  type GroupPackageProduct,
} from '../../api/group-packages'
import { ApiError } from '../../api/http'
import { useAuthStore } from '../../stores/auth'

const authStore = useAuthStore()
const router = useRouter()
const memberId = computed(() => authStore.session?.userId)

const courses = ref<GroupCourse[]>([])
const products = ref<GroupPackageProduct[]>([])
const ownedTypeIds = ref<number[]>([])
const loading = ref(true)
const error = ref('')
const message = ref('')
const messageType = ref<'success' | 'error'>('success')
const buyingPriceId = ref<number | null>(null)

async function loadData() {
  loading.value = true
  error.value = ''
  try {
    const tasks: Promise<unknown>[] = [
      getGroupCourses().then((data) => {
        courses.value = data
      }),
      getGroupPackageProducts().then((data) => {
        products.value = data
      }),
    ]

    if (memberId.value) {
      tasks.push(
        getMyGroupPackages(memberId.value).then((pkgs) => {
          ownedTypeIds.value = [
            ...new Set(
              pkgs
                .filter((p) => p.packageStatus !== '2')
                .map((p) => p.typeId)
                .filter((id) => id > 0),
            ),
          ]
        }),
      )
    } else {
      ownedTypeIds.value = []
    }

    await Promise.all(tasks)
  } catch (err) {
    error.value = err instanceof Error ? err.message : '加载失败，请稍后重试'
  } finally {
    loading.value = false
  }
}

async function handlePurchase(product: GroupPackageProduct) {
  if (!memberId.value) {
    messageType.value = 'error'
    message.value = '请先登录会员账号'
    return
  }

  const ok = window.confirm(
    `确认购买「${product.name}」？\n价格 ¥${product.price}\n将创建待支付订单，请到「我的订单」完成支付。`,
  )
  if (!ok) return

  buyingPriceId.value = product.priceId
  message.value = ''
  try {
    await purchaseGroupPackage({
      memberId: memberId.value,
      priceId: product.priceId,
    })
    messageType.value = 'success'
    message.value = '下单成功，即将跳转我的订单完成支付'
    setTimeout(() => router.push('/member/orders'), 800)
  } catch (err) {
    messageType.value = 'error'
    message.value = err instanceof ApiError ? err.message : '下单失败'
  } finally {
    buyingPriceId.value = null
  }
}

onMounted(loadData)
</script>

<template>
  <div class="group-course-page">
    <PageHeader
      eyebrow="GROUP COURSES"
      title="团课课包"
      subtitle="您可以在此查看本周全部团课安排并购买心仪课包。购买成功后请到「我的团课」预约。"
    >
      <template #actions>
        <RouterLink class="ghost-link" to="/member/my-group-bookings">我的团课</RouterLink>
        <RouterLink class="ghost-link" to="/member/orders">我的订单</RouterLink>
      </template>
    </PageHeader>

    <p v-if="message" class="booking-message" :class="messageType">{{ message }}</p>
    <StateCard v-if="loading" message="正在加载..." />
    <StateCard v-else-if="error" :message="error" type="error" />

    <template v-else>
      <section class="purchase-section panel-tone-blue">
        <div class="section-head">
          <h2>购买团课课包</h2>
          <p>按课程类型购买次数包，支付成功后在「我的团课」查看。</p>
        </div>

        <StateCard v-if="products.length === 0" message="暂无在售团课课包，请联系员工上架。" />

        <div v-else class="product-grid">
          <article v-for="product in products" :key="product.priceId" class="product-card">
            <h3>{{ product.name }}</h3>
            <p class="meta">{{ product.courseTypeName }} · {{ product.sessionCount }} 次</p>
            <strong class="price">¥{{ product.price.toFixed(2) }}</strong>
            <button
              type="button"
              class="booking-button"
              :disabled="buyingPriceId === product.priceId"
              @click="handlePurchase(product)"
            >
              {{ buyingPriceId === product.priceId ? '下单中...' : '购买课包' }}
            </button>
          </article>
        </div>
      </section>

      <section class="schedule-section">
        <div class="section-head">
          <h2>团课安排</h2>
          <p>按周展示全部团课；蓝色为您已购课包类型，灰色为未购。</p>
        </div>
        <GroupCourseTimetable :courses="courses" :owned-type-ids="ownedTypeIds" />
        <div class="schedule-actions">
          <RouterLink class="booking-button link-btn" to="/member/my-group-bookings">
            去我的团课预约
          </RouterLink>
        </div>
      </section>
    </template>
  </div>
</template>

<style scoped>
.group-course-page { width: 100%; }
.ghost-link {
  display: inline-flex; margin-left: 8px; padding: 10px 16px; border-radius: 999px;
  border: 1px solid rgba(42, 67, 101, 0.18); color: #2a4365; text-decoration: none; font-weight: 600;
  background: #eaf5f6;
}
.booking-message { margin-bottom: 16px; padding: 12px 16px; border-radius: 20px; font-weight: 600; }
.booking-message.success { background: #e8f4f5; color: #2a4365; }
.booking-message.error { background: #fcebed; color: var(--tj-danger); }
.section-head { margin: 8px 0 16px; }
.section-head h2 { margin: 0 0 6px; }
.section-head p { margin: 0; color: #72819a; }
.purchase-section, .schedule-section { margin-bottom: 28px; }
.purchase-section.panel-tone-blue { padding: 22px; border-radius: 28px; }
.product-grid {
  display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 16px;
}
.product-card {
  padding: 20px; border-radius: 28px;
  display: flex; flex-direction: column; gap: 10px;
  /* 背景色由父级 panel-tone-* 提供 */
}
.price { color: #2a4365; font-size: 22px; }
.meta { color: #72819a; }
.booking-button, .link-btn {
  margin-top: auto; padding: 10px 14px; border: none; border-radius: 999px;
  background: #2a4365; color: #fff; font-weight: 600; cursor: pointer; text-align: center; text-decoration: none;
}
.booking-button:disabled { opacity: 0.6; cursor: not-allowed; }
.schedule-actions { margin-top: 16px; display: flex; }
</style>
