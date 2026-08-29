<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ApiError } from '@/api/http'
import {
  cancelPaymentOrder,
  getPaymentOrders,
  payPaymentOrder,
  updateOrderVoucher,
  type PaymentOrder,
} from '@/api/payment-orders'
import { getAvailableVouchers, type Voucher } from '@/api/vouchers'
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

const orders = ref<PaymentOrder[]>([])
const loading = ref(true)
const errorMessage = ref('')
const notice = ref('')

const showPayDialog = ref(false)
const activeOrder = ref<PaymentOrder | null>(null)
const availableVouchers = ref<Voucher[]>([])
const selectedVoucherId = ref<number | null>(null)
const busy = ref(false)
const dialogError = ref('')

const isAdmin = computed(() => props.mode === 'admin')

const currentMemberId = computed(() => {
  if (authStore.session?.userType === 'member') {
    return authStore.session.userId
  }
  return PREVIEW_MEMBER_ID
})

const memberIdForQuery = computed(() => (isAdmin.value ? undefined : currentMemberId.value))

const previewPayable = computed(() => {
  const total = activeOrder.value?.totalAmount ?? 0
  const voucher = availableVouchers.value.find((item) => item.voucherId === selectedVoucherId.value)
  const discount = voucher?.discountValue ?? 0
  return Math.max(total - discount, 0)
})

const previewDiscount = computed(() => {
  const voucher = availableVouchers.value.find((item) => item.voucherId === selectedVoucherId.value)
  return voucher?.discountValue ?? 0
})

function formatMoney(value: number) {
  return `¥${value.toFixed(2)}`
}

function formatDateTime(value?: string) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleString('zh-CN')
}

function isPending(order: PaymentOrder) {
  return order.paymentStatus?.trim() === '待支付'
}

function isPaid(order: PaymentOrder) {
  return order.paymentStatus?.trim() === '已支付'
}

function canCancel(order: PaymentOrder) {
  return isPending(order) || isPaid(order)
}

function statusClass(status?: string) {
  const value = status?.trim()
  if (value === '待支付') return 'pending'
  if (value === '已支付') return 'paid'
  if (value === '已取消') return 'cancelled'
  return ''
}

function replaceOrder(updated: PaymentOrder) {
  const index = orders.value.findIndex((item) => item.orderId === updated.orderId)
  if (index >= 0) {
    orders.value[index] = updated
  } else {
    orders.value.unshift(updated)
  }
}

async function loadOrders() {
  loading.value = true
  errorMessage.value = ''
  try {
    orders.value = await getPaymentOrders({
      memberId: memberIdForQuery.value,
      pageNumber: 1,
      pageSize: 50,
    })
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '订单加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

async function openPayDialog(order: PaymentOrder) {
  if (!isPending(order)) return
  dialogError.value = ''
  showPayDialog.value = true
  activeOrder.value = order
  selectedVoucherId.value = order.voucherId ?? null
  try {
    availableVouchers.value = await getAvailableVouchers(currentMemberId.value, order.orderId)
    if (
      selectedVoucherId.value != null &&
      !availableVouchers.value.some((item) => item.voucherId === selectedVoucherId.value)
    ) {
      selectedVoucherId.value = availableVouchers.value[0]?.voucherId ?? null
    }
  } catch (error) {
    dialogError.value = error instanceof ApiError ? error.message : '加载可用优惠券失败。'
  }
}

function closeDialog() {
  if (busy.value) return
  showPayDialog.value = false
  activeOrder.value = null
  dialogError.value = ''
}

async function applyVoucherChange() {
  if (!activeOrder.value) return
  const updated = await updateOrderVoucher(
    activeOrder.value.orderId,
    selectedVoucherId.value,
    activeOrder.value.memberId ?? currentMemberId.value,
  )
  replaceOrder(updated)
  activeOrder.value = updated
}

async function confirmPay() {
  if (!activeOrder.value) return
  busy.value = true
  dialogError.value = ''
  try {
    if ((activeOrder.value.voucherId ?? null) !== selectedVoucherId.value) {
      await applyVoucherChange()
    }
    const updated = await payPaymentOrder(activeOrder.value.orderId)
    replaceOrder(updated)
    notice.value = `订单 #${updated.orderId} 支付成功，实付 ${formatMoney(updated.payableAmount)}。`
    closeDialog()
  } catch (error) {
    dialogError.value = error instanceof ApiError ? error.message : '支付失败，请稍后重试。'
  } finally {
    busy.value = false
  }
}

async function handleCancel(order: PaymentOrder) {
  const tip = isPaid(order)
    ? `确认取消已支付订单 #${order.orderId}？将退回实付 ${formatMoney(order.payableAmount)}，优惠券不退。`
    : `确认取消待支付订单 #${order.orderId}？`
  if (!window.confirm(tip)) return

  try {
    const updated = await cancelPaymentOrder(order.orderId)
    replaceOrder(updated)
    notice.value = updated.actionMessage || `订单 #${updated.orderId} 已取消。`
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '取消订单失败。'
  }
}

onMounted(loadOrders)
</script>

<template>
  <div class="payment-orders">
    <PageHeader
      eyebrow="Payment · H"
      :title="isAdmin ? '订单管理' : '我的订单'"
      :subtitle="
        isAdmin
          ? '员工端查看订单；待支付可支付/取消，已支付可取消（退实付不退券）。'
          : '订单由团课/私教等业务页生成。待支付可改券后支付或取消；已支付可取消（退实付不退券）。'
      "
    >
      <template #actions>
        <button type="button" class="ghost-btn" :disabled="loading" @click="loadOrders">刷新</button>
      </template>
    </PageHeader>

    <p v-if="notice" class="success-banner">{{ notice }}</p>

    <StateCard v-if="loading" message="订单加载中..." />
    <StateCard v-else-if="errorMessage" type="error" :message="errorMessage" />
    <StateCard v-else-if="orders.length === 0" message="暂无订单。请从团课预约或私教课包等业务页完成下单后再来支付。" />

    <div v-else class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>订单号</th>
            <th>业务单号</th>
            <th v-if="isAdmin">会员ID</th>
            <th>原价</th>
            <th>优惠券</th>
            <th>优惠</th>
            <th>应付</th>
            <th>状态</th>
            <th>创建时间</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="order in orders" :key="order.orderId">
            <td>{{ order.orderId }}</td>
            <td>{{ order.businessOrderId }}</td>
            <td v-if="isAdmin">{{ order.memberId ?? '—' }}</td>
            <td>{{ formatMoney(order.totalAmount) }}</td>
            <td>{{ order.voucherType || (order.voucherId ? `#${order.voucherId}` : '未用券') }}</td>
            <td>{{ formatMoney(order.discountValue) }}</td>
            <td class="payable">{{ formatMoney(order.payableAmount) }}</td>
            <td>
              <span class="badge" :class="statusClass(order.paymentStatus)">{{ order.paymentStatus || '未知' }}</span>
            </td>
            <td>{{ formatDateTime(order.createTime) }}</td>
            <td class="actions-cell">
              <button v-if="isPending(order)" type="button" class="pay-btn" @click="openPayDialog(order)">去支付</button>
              <button v-if="canCancel(order)" type="button" class="cancel-btn" @click="handleCancel(order)">取消订单</button>
              <span v-if="!isPending(order) && !canCancel(order)" class="muted">—</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="showPayDialog && activeOrder" class="modal-mask" @click.self="closeDialog">
      <section class="pay-modal" role="dialog" aria-modal="true">
        <header>
          <p class="eyebrow">确认支付</p>
          <h2>支付订单 #{{ activeOrder.orderId }}</h2>
        </header>

        <label class="field">
          <span>优惠券（一次一张；可更改）</span>
          <select v-model="selectedVoucherId">
            <option :value="null">不使用优惠券</option>
            <option v-for="voucher in availableVouchers" :key="voucher.voucherId" :value="voucher.voucherId">
              {{ voucher.voucherType }} · 减{{ formatMoney(voucher.discountValue) }} · 至
              {{ new Date(voucher.validUntil).toLocaleDateString('zh-CN') }}
            </option>
          </select>
        </label>

        <dl class="pay-summary">
          <div>
            <dt>原价</dt>
            <dd>{{ formatMoney(activeOrder.totalAmount) }}</dd>
          </div>
          <div>
            <dt>优惠抵扣</dt>
            <dd>-{{ formatMoney(previewDiscount) }}</dd>
          </div>
          <div class="total">
            <dt>应付金额</dt>
            <dd>{{ formatMoney(previewPayable) }}</dd>
          </div>
        </dl>

        <p class="hint">选券规则：优惠最多优先；同额则优先马上过期。支付后取消仅退实付，不退券。</p>
        <p v-if="dialogError" class="pay-error">{{ dialogError }}</p>

        <footer class="modal-actions">
          <button type="button" class="ghost-btn" :disabled="busy" @click="closeDialog">关闭</button>
          <button type="button" class="primary-btn" :disabled="busy" @click="confirmPay">
            {{ busy ? '支付中...' : '确认支付' }}
          </button>
        </footer>
      </section>
    </div>
  </div>
</template>

<style scoped>
.ghost-btn,
.primary-btn,
.pay-btn,
.cancel-btn {
  border-radius: 999px;
  padding: 10px 18px;
  cursor: pointer;
  font: inherit;
}

.ghost-btn {
  border: 1px solid #c9d6ef;
  background: #fff;
  color: var(--tj-text);
}

.primary-btn,
.pay-btn {
  border: none;
  background: var(--tj-primary);
  color: #fff;
}

.pay-btn,
.cancel-btn {
  padding: 6px 12px;
  font-size: 13px;
}

.cancel-btn {
  border: 1px solid #f0c2c8;
  background: #fff;
  color: var(--tj-danger);
}

.ghost-btn:disabled,
.primary-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.success-banner {
  margin: 0 0 16px;
  padding: 12px 16px;
  border-radius: 12px;
  background: #e7f8ed;
  color: #1f8f4e;
}

.table-wrap {
  overflow: auto;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

table {
  width: 100%;
  border-collapse: collapse;
  min-width: 920px;
}

th,
td {
  padding: 14px 16px;
  text-align: left;
  border-bottom: 1px solid #e8eef8;
  font-size: 14px;
}

th {
  color: var(--tj-text-muted);
  font-weight: 600;
  background: #f8fbff;
}

.payable {
  font-weight: 700;
  color: var(--tj-primary);
}

.badge {
  display: inline-block;
  padding: 4px 10px;
  border-radius: 999px;
  background: var(--tj-primary-soft);
  color: var(--tj-primary);
  font-size: 12px;
}

.badge.pending {
  background: #fff4e5;
  color: #c56b00;
}

.badge.paid {
  background: #e7f8ed;
  color: #1f8f4e;
}

.badge.cancelled {
  background: #fdecef;
  color: var(--tj-danger);
}

.actions-cell {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.muted {
  color: var(--tj-text-muted);
}

.modal-mask {
  position: fixed;
  inset: 0;
  z-index: 40;
  display: grid;
  place-items: center;
  padding: 24px;
  background: rgba(17, 28, 49, 0.45);
}

.pay-modal {
  width: min(460px, 100%);
  padding: 24px;
  border-radius: 20px;
  background: #fff;
  box-shadow: 0 24px 60px rgba(17, 28, 49, 0.2);
}

.eyebrow {
  margin: 0 0 8px;
  font-size: 12px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: #4d77ff;
}

.pay-modal h2 {
  margin: 0;
  font-size: 22px;
  color: var(--tj-text);
}

.field {
  display: grid;
  gap: 8px;
  margin-top: 16px;
  font-size: 13px;
  color: var(--tj-text-muted);
}

.field select {
  padding: 10px 12px;
  border: 1px solid #c9d6ef;
  border-radius: 10px;
  font: inherit;
  color: var(--tj-text);
  background: #fff;
}

.pay-summary {
  margin: 18px 0 0;
  display: grid;
  gap: 10px;
}

.pay-summary > div {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  font-size: 14px;
}

.pay-summary dt {
  color: var(--tj-text-muted);
}

.pay-summary dd {
  margin: 0;
}

.pay-summary .total {
  margin-top: 6px;
  padding-top: 12px;
  border-top: 1px solid #e8eef8;
  font-size: 16px;
  font-weight: 700;
}

.pay-summary .total dd {
  color: var(--tj-primary);
}

.hint {
  margin: 14px 0 0;
  color: var(--tj-text-muted);
  font-size: 13px;
  line-height: 1.6;
}

.pay-error {
  margin: 12px 0 0;
  color: var(--tj-danger);
  font-size: 13px;
}

.modal-actions {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}
</style>
