<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { cancelMember, getMemberProfile, type MemberProfile } from '@/api/members'
import StateCard from '@/components/ui/StateCard.vue'

const route = useRoute()
const router = useRouter()
const loading = ref(true)
const cancelling = ref(false)
const errorMessage = ref('')
const member = ref<MemberProfile | null>(null)

const memberId = computed(() => Number(route.params.id))

function formatDate(value?: string) {
  if (!value) return '未填写'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '未填写' : date.toLocaleDateString('zh-CN')
}

function resolveStatusLabel(value?: string) {
  if (value === '1') return '有效'
  if (value === '0') return '已注销'
  return value || '未填写'
}

function resolveGenderLabel(value?: string) {
  if (value === 'M') return '男'
  if (value === 'F') return '女'
  return value || '未填写'
}

async function loadDetail() {
  loading.value = true
  errorMessage.value = ''

  try {
    member.value = await getMemberProfile(memberId.value)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '会员详情加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

async function handleCancel() {
  if (!member.value) return
  if (!confirm(`确定要注销会员「${member.value.name}」吗？`)) return

  cancelling.value = true
  errorMessage.value = ''

  try {
    member.value = await cancelMember(member.value.memberId)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '注销失败，请稍后重试。'
  } finally {
    cancelling.value = false
  }
}

onMounted(loadDetail)
</script>

<template>
  <div class="member-detail-view">
    <section class="page-head">
      <div>
        <p class="eyebrow">管理员端</p>
        <h1>会员详情卡片</h1>
      </div>
      <div class="head-actions">
        <button type="button" class="btn-ghost" @click="router.push('/admin/members')">返回列表</button>
        <button type="button" class="btn-danger" :disabled="cancelling || member?.status === '0'" @click="handleCancel">
          {{ cancelling ? '注销中...' : '注销会员' }}
        </button>
      </div>
    </section>

    <StateCard v-if="errorMessage" :message="errorMessage" type="error" />
    <div v-else-if="loading" class="loading-state">加载中...</div>

    <section v-else-if="member" class="detail-card">
      <div class="title-row">
        <div>
          <h2>{{ member.name }}</h2>
          <p>会员编号 #{{ member.memberId }}</p>
        </div>
        <span class="status-chip">{{ resolveStatusLabel(member.status) }}</span>
      </div>

      <div class="detail-grid">
        <div class="detail-item">
          <span class="detail-label">手机号</span>
          <strong class="detail-value">{{ member.phoneNumber || '未填写' }}</strong>
        </div>
        <div class="detail-item">
          <span class="detail-label">会员等级</span>
          <strong class="detail-value">{{ member.memberLevel || '未填写' }}</strong>
        </div>
        <div class="detail-item">
          <span class="detail-label">性别</span>
          <strong class="detail-value">{{ resolveGenderLabel(member.gender) }}</strong>
        </div>
        <div class="detail-item">
          <span class="detail-label">生日</span>
          <strong class="detail-value">{{ formatDate(member.birthday) }}</strong>
        </div>
        <div class="detail-item full-width">
          <span class="detail-label">身份证号</span>
          <strong class="detail-value">{{ member.idCard || '未填写' }}</strong>
        </div>
        <div class="detail-item">
          <span class="detail-label">注册时间</span>
          <strong class="detail-value">{{ formatDate(member.registerDate) }}</strong>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
.member-detail-view {
  display: grid;
  gap: 18px;
}

.page-head,
.detail-card {
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 14px;
  padding: 18px;
}

.page-head {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: center;
}

.eyebrow,
.title-row p {
  margin: 0;
  color: #6b7280;
}

.page-head h1,
.title-row h2 {
  margin: 4px 0;
}

.head-actions {
  display: flex;
  gap: 10px;
}

.title-row {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
  padding-bottom: 16px;
  border-bottom: 1px solid #f1f5f9;
}

.status-chip {
  background: #eef2ff;
  color: #4338ca;
  border-radius: 999px;
  padding: 6px 10px;
  font-size: 13px;
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 18px;
  margin-top: 18px;
}

.detail-item {
  display: grid;
  gap: 10px;
  padding: 14px 16px;
  border-radius: 14px;
  background: #f8fbff;
  border: 1px solid #e6edf8;
}

.detail-item.full-width {
  grid-column: 1 / -1;
}

.detail-label {
  color: #64748b;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.detail-value {
  color: #0f172a;
  font-size: 15px;
  font-weight: 700;
  line-height: 1.5;
}

.btn-ghost,
.btn-danger {
  border: none;
  border-radius: 10px;
  padding: 10px 14px;
  cursor: pointer;
}

.btn-ghost {
  background: #eff6ff;
  color: #2563eb;
}

.btn-danger {
  background: #fee2e2;
  color: #b91c1c;
}

.loading-state {
  padding: 28px;
  text-align: center;
  color: #6b7280;
}

@media (max-width: 860px) {
  .page-head,
  .detail-grid {
    grid-template-columns: 1fr;
  }

  .page-head,
  .title-row {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
