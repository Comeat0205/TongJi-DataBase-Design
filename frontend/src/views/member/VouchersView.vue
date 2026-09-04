<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ApiError } from '@/api/http'
import { getVouchers, type Voucher } from '@/api/vouchers'
import { PREVIEW_MEMBER_ID } from '@/config/nav'
import { useAuthStore } from '@/stores/auth'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'

const props = withDefaults(
  defineProps<{
    mode?: 'member' | 'admin'
  }>(),
  { mode: 'member' },
)

const route = useRoute()
const authStore = useAuthStore()

const vouchers = ref<Voucher[]>([])
const loading = ref(true)
const errorMessage = ref('')

const isAdmin = computed(() => props.mode === 'admin')
const isPreview = computed(() => route.path.startsWith('/preview/'))

const memberId = computed(() => {
  if (isAdmin.value) {
    return undefined
  }
  return authStore.session?.userType === 'member'
    ? authStore.session.userId
    : PREVIEW_MEMBER_ID
})

function formatMoney(value: number) {
  return `¥${value.toFixed(2)}`
}

function formatDate(value?: string) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleDateString('zh-CN')
}

function voucherHint(type: string) {
  if (type === '生日福利券') return '生日当天发放 · 生日起 1 个月内有效 · ¥66'
  if (type === '新客体验券') return '注册即领 · 注册日起 1 年内有效 · ¥50'
  if (type === '折扣券') return '员工发放 · 领取起 7 天内有效 · ¥33'
  return ''
}

async function loadVouchers() {
  loading.value = true
  errorMessage.value = ''
  try {
    vouchers.value = await getVouchers({
      memberId: memberId.value,
      pageNumber: 1,
      pageSize: 50,
    })
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '优惠券加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

onMounted(loadVouchers)
</script>

<template>
  <div class="vouchers-page">
    <PageHeader
      eyebrow="Marketing · H"
      :title="isAdmin ? '优惠券管理' : '我的优惠券'"
      :subtitle="
        isAdmin
          ? '员工端仅管理折扣券发放。'
          : isPreview
            ? `预览模式：演示会员 ID ${PREVIEW_MEMBER_ID} 的优惠券。`
            : '生日福利券、新客体验券、员工折扣券。'
      "
    >
      <template #actions>
        <button type="button" class="ghost-btn" :disabled="loading" @click="loadVouchers">刷新</button>
      </template>
    </PageHeader>

    <StateCard v-if="loading" message="优惠券加载中..." />
    <StateCard v-else-if="errorMessage" type="error" :message="errorMessage" />
    <StateCard v-else-if="vouchers.length === 0" message="暂无优惠券。" />

    <div v-else class="card-grid">
      <article
        v-for="voucher in vouchers"
        :key="voucher.voucherId"
        class="voucher-card"
        :class="{ expired: voucher.isExpired || voucher.statusText === '过期作废' }"
      >
        <div class="top">
          <h2>{{ voucher.voucherType }}</h2>
          <span
            class="status"
            :data-status="voucher.isExpired || voucher.statusText === '过期作废' ? '2' : voucher.status"
          >
            {{ voucher.statusText }}
          </span>
        </div>
        <p class="discount">{{ formatMoney(voucher.discountValue) }}</p>
        <dl>
          <div>
            <dt>券编号</dt>
            <dd>{{ voucher.voucherId }}</dd>
          </div>
          <div v-if="isAdmin">
            <dt>会员ID</dt>
            <dd>{{ voucher.memberId }}</dd>
          </div>
          <div>
            <dt>有效期至</dt>
            <dd>{{ formatDate(voucher.validUntil) }}</dd>
          </div>
        </dl>
        <p v-if="voucherHint(voucher.voucherType)" class="hint">{{ voucherHint(voucher.voucherType) }}</p>
      </article>
    </div>
  </div>
</template>

<style scoped>
.ghost-btn {
  border: 1px solid #c9d6ef;
  background: #fff;
  color: var(--tj-text);
  border-radius: 999px;
  padding: 10px 18px;
  cursor: pointer;
}

.card-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 16px;
}

.voucher-card {
  position: relative;
  padding: 20px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.voucher-card.expired {
  opacity: 0.78;
}

.voucher-card.expired .discount {
  color: var(--tj-text-muted);
}

.status[data-status='2'] {
  background: #fdecef;
  color: var(--tj-danger);
}

.top {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
}

.voucher-card h2 {
  margin: 0;
  font-size: 18px;
  color: var(--tj-text);
}

.status {
  flex-shrink: 0;
  padding: 4px 10px;
  border-radius: 999px;
  background: #eef2f8;
  color: var(--tj-text-muted);
  font-size: 12px;
}

.status[data-status='0'] {
  background: #e7f8ed;
  color: #1f8f4e;
}

.status[data-status='1'] {
  background: #e8eef8;
  color: #4a5d7a;
}

.discount {
  margin: 16px 0 12px;
  font-size: 28px;
  font-weight: 700;
  color: var(--tj-primary);
}

dl {
  margin: 0;
  display: grid;
  gap: 8px;
}

dl > div {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  font-size: 13px;
}

dt {
  color: var(--tj-text-muted);
}

dd {
  margin: 0;
  color: var(--tj-text);
}

.hint {
  margin: 14px 0 0;
  font-size: 12px;
  color: #4d77ff;
}
</style>
