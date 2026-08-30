<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ApiError } from '@/api/http'
import { getMembers, type MemberProfile } from '@/api/members'
import { getVouchers, issueDiscountVoucher, issueDiscountVoucherToAll, VOUCHER_TYPE_DISCOUNT, type Voucher } from '@/api/vouchers'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'

const vouchers = ref<Voucher[]>([])
const members = ref<MemberProfile[]>([])
const loading = ref(true)
const issuing = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const selectedMemberId = ref<number | ''>('')

async function loadVouchers() {
  loading.value = true
  errorMessage.value = ''
  try {
    vouchers.value = await getVouchers({
      voucherType: VOUCHER_TYPE_DISCOUNT,
      pageNumber: 1,
      pageSize: 100,
    })
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '折扣券加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

async function loadMembers() {
  try {
    members.value = await getMembers({ pageNumber: 1, pageSize: 200 })
  } catch {
    members.value = []
  }
}

const memberNameMap = computed(() => {
  const map = new Map<number, string>()
  for (const member of members.value) {
    map.set(member.memberId, member.name)
  }
  return map
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

function memberLabel(memberId: number) {
  const name = memberNameMap.value.get(memberId)
  return name ? `${name} (#${memberId})` : `#${memberId}`
}

async function submitIssue() {
  if (selectedMemberId.value === '') {
    errorMessage.value = '请选择要发放折扣券的会员。'
    return
  }

  issuing.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    const voucher = await issueDiscountVoucher(Number(selectedMemberId.value))
    successMessage.value = `已向 ${memberLabel(voucher.memberId)} 发放折扣券（¥33，7 天有效）。`
    selectedMemberId.value = ''
    await loadVouchers()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '发放失败，请稍后重试。'
  } finally {
    issuing.value = false
  }
}

async function submitIssueAll() {
  if (!window.confirm('确认给每位在籍会员发放一张 ¥33 折扣券（7 天有效）？')) return

  issuing.value = true
  errorMessage.value = ''
  successMessage.value = ''
  try {
    const count = await issueDiscountVoucherToAll()
    successMessage.value = `已向 ${count} 名会员各发放一张折扣券（¥33，7 天有效）。`
    await loadVouchers()
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '全体发放失败，请稍后重试。'
  } finally {
    issuing.value = false
  }
}

onMounted(async () => {
  await Promise.all([loadMembers(), loadVouchers()])
})
</script>

<template>
  <div class="admin-vouchers-page">
    <PageHeader
      eyebrow="Marketing · H"
      title="折扣券发放"
      subtitle="员工可向指定会员发放 33 元折扣券，自发放起 7 天内有效。"
    >
      <template #actions>
        <button type="button" class="ghost-btn" :disabled="loading" @click="loadVouchers">刷新</button>
      </template>
    </PageHeader>

    <section class="issue-panel">
      <h2>发放折扣券</h2>
      <p class="hint">系统仅支持三种券：生日福利券、新客体验券、员工折扣券。此处仅管理折扣券发放。</p>
      <div class="issue-row">
        <label>
          发放给会员
          <select v-model="selectedMemberId" :disabled="issuing">
            <option value="">请选择会员</option>
            <option v-for="member in members" :key="member.memberId" :value="member.memberId">
              {{ member.name }} (#{{ member.memberId }})
            </option>
          </select>
        </label>
        <button type="button" class="primary-btn" :disabled="issuing" @click="submitIssue">
          {{ issuing ? '发放中…' : '发放 ¥33 折扣券' }}
        </button>
        <button type="button" class="ghost-btn" :disabled="issuing" @click="submitIssueAll">
          给全体会员各发一张
        </button>
      </div>
      <p v-if="successMessage" class="success">{{ successMessage }}</p>
    </section>

    <StateCard v-if="loading" message="折扣券加载中..." />
    <StateCard v-else-if="errorMessage && vouchers.length === 0" type="error" :message="errorMessage" />
    <StateCard v-else-if="vouchers.length === 0" message="暂无已发放的折扣券。" />

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
          <div>
            <dt>会员</dt>
            <dd>{{ memberLabel(voucher.memberId) }}</dd>
          </div>
          <div>
            <dt>有效期至</dt>
            <dd>{{ formatDate(voucher.validUntil) }}</dd>
          </div>
        </dl>
      </article>
    </div>

    <p v-if="errorMessage && vouchers.length > 0" class="inline-error">{{ errorMessage }}</p>
  </div>
</template>

<style scoped>
.issue-panel {
  margin-bottom: 20px;
  padding: 20px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.issue-panel h2 {
  margin: 0 0 8px;
  font-size: 18px;
}

.hint {
  margin: 0 0 16px;
  color: var(--tj-text-muted);
  font-size: 13px;
}

.issue-row {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: flex-end;
}

label {
  display: grid;
  gap: 6px;
  font-size: 13px;
  color: var(--tj-text-muted);
}

select {
  min-width: 220px;
  padding: 10px 12px;
  border: 1px solid #c9d6ef;
  border-radius: 10px;
  background: #fff;
}

.primary-btn,
.ghost-btn {
  border-radius: 999px;
  padding: 10px 18px;
  cursor: pointer;
}

.primary-btn {
  border: none;
  background: var(--tj-primary);
  color: #fff;
}

.ghost-btn {
  border: 1px solid #c9d6ef;
  background: #fff;
  color: var(--tj-text);
}

.success {
  margin: 12px 0 0;
  color: #1f8f4e;
  font-size: 13px;
}

.inline-error {
  margin-top: 12px;
  color: var(--tj-danger);
  font-size: 13px;
}

.card-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 16px;
}

.voucher-card {
  padding: 20px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.voucher-card.expired {
  opacity: 0.78;
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
}

.status {
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

.status[data-status='2'] {
  background: #fdecef;
  color: var(--tj-danger);
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
}
</style>
