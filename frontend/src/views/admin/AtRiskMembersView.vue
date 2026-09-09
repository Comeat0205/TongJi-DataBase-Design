<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ApiError } from '@/api/http'
import { getAtRiskMembers, type AtRiskMember } from '@/api/at-risk-members'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'

const members = ref<AtRiskMember[]>([])
const loading = ref(true)
const errorMessage = ref('')
const inactiveDays = ref(1)

function formatDateTime(value?: string) {
  if (!value) return '从未入场'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleDateString('zh-CN')
}

async function loadMembers() {
  loading.value = true
  errorMessage.value = ''
  try {
    members.value = await getAtRiskMembers({
      inactiveDays: inactiveDays.value,
      pageNumber: 1,
      pageSize: 50,
    })
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '流失预警加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

onMounted(loadMembers)
</script>

<template>
  <div class="at-risk-page">
    <PageHeader
      eyebrow="At Risk Members"
      title="流失预警会员"
      subtitle="仅统计有签到记录的会员：未活跃天数 = 今天 − 最后签到日。"
    >
      <template #actions>
        <form class="filter-form" @submit.prevent="loadMembers">
          <label class="days-filter">
            未入场天数 ≥
            <input v-model.number="inactiveDays" type="number" min="1" max="365" />
          </label>
          <button type="submit" class="ghost-btn" :disabled="loading">查询</button>
        </form>
      </template>
    </PageHeader>

    <StateCard v-if="loading" message="流失预警加载中..." />
    <StateCard v-else-if="errorMessage" type="error" :message="errorMessage" />
    <StateCard v-else-if="members.length === 0" message="当前条件下暂无流失预警会员。" />

    <div v-else class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>会员ID</th>
            <th>姓名</th>
            <th>手机号</th>
            <th>等级</th>
            <th>最近签到日</th>
            <th>未活跃天数</th>
            <th>未用券数</th>
            <th>预警原因</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="member in members" :key="member.memberId">
            <td>{{ member.memberId }}</td>
            <td>{{ member.name }}</td>
            <td>{{ member.phoneNumber || '—' }}</td>
            <td>{{ member.memberLevel || '—' }}</td>
            <td>{{ formatDateTime(member.lastCheckInTime) }}</td>
            <td>
              <span class="badge danger">{{ member.inactiveDays }} 天</span>
            </td>
            <td>{{ member.unusedVoucherCount }}</td>
            <td>{{ member.riskReason }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.days-filter {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  color: var(--tj-text-muted);
  font-size: 14px;
}

.filter-form {
  display: inline-flex;
  align-items: center;
  gap: 10px;
}

.days-filter input {
  width: 72px;
  padding: 8px 10px;
  border: 1px solid #c9d6ef;
  border-radius: 10px;
}

.ghost-btn {
  border: 1px solid #c9d6ef;
  background: #fff;
  color: var(--tj-text);
  border-radius: 999px;
  padding: 10px 18px;
  cursor: pointer;
}

.table-wrap {
  overflow: auto;
  border-radius: 24px;
  background: #eef4fc;
  box-shadow: var(--tj-member-lift);
  border: var(--tj-member-edge);
}

table {
  width: 100%;
  border-collapse: collapse;
  min-width: 860px;
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
  background: #eaf5f6;
}

tbody tr:nth-child(odd) td {
  background: #eef4fc;
}

tbody tr:nth-child(even) td {
  background: #eaf5f6;
}

.badge.danger {
  display: inline-block;
  padding: 4px 10px;
  border-radius: 999px;
  background: #fdecef;
  color: var(--tj-danger);
  font-size: 12px;
}
</style>
