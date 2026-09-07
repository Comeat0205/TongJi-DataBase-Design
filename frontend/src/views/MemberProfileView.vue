<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '../api/http'
import { getMemberProfile, type MemberProfile } from '../api/members'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const profile = ref<MemberProfile | null>(null)
const loading = ref(true)
const errorMessage = ref('')

const memberId = computed(() => Number(route.params.id))

function formatDate(value?: string) {
  if (!value) {
    return '未填写'
  }

  return new Date(value).toLocaleDateString('zh-CN')
}

async function loadProfile() {
  loading.value = true
  errorMessage.value = ''

  try {
    profile.value = await getMemberProfile(memberId.value)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '会员档案加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

function logout() {
  authStore.clearSession()
  router.push('/login')
}

onMounted(loadProfile)
</script>

<template>
  <main class="profile-page">
    <section class="profile-shell">
      <header class="profile-header">
        <div>
          <p class="eyebrow">Member Portal</p>
          <h1>会员档案</h1>
          <p class="subtitle">登录成功后，当前先跳转到会员档案页，后续可继续扩展预约、卡券与课程功能。</p>
        </div>
        <div class="header-actions">
          <div class="welcome-card">
            <span>当前登录</span>
            <strong>{{ authStore.session?.displayName ?? '会员' }}</strong>
          </div>
          <button type="button" class="logout-btn" @click="logout">退出登录</button>
        </div>
      </header>

      <section v-if="loading" class="state-card">会员档案加载中...</section>
      <section v-else-if="errorMessage" class="state-card error">{{ errorMessage }}</section>

      <section v-else-if="profile" class="profile-grid">
        <article class="profile-card summary-card">
          <p class="summary-label">会员编号</p>
          <h2>#{{ profile.memberId }}</h2>
          <p class="summary-name">{{ profile.name }}</p>
          <span class="summary-status">{{ profile.status || '状态未知' }}</span>
        </article>

        <article class="profile-card detail-card">
          <div class="detail-row">
            <span>手机号</span>
            <strong>{{ profile.phoneNumber || '未填写' }}</strong>
          </div>
          <div class="detail-row">
            <span>会员等级</span>
            <strong>{{ profile.memberLevel || '未设置' }}</strong>
          </div>
          <div class="detail-row">
            <span>性别</span>
            <strong>{{ profile.gender || '未填写' }}</strong>
          </div>
          <div class="detail-row">
            <span>生日</span>
            <strong>{{ formatDate(profile.birthday) }}</strong>
          </div>
          <div class="detail-row">
            <span>注册时间</span>
            <strong>{{ formatDate(profile.registerDate) }}</strong>
          </div>
          <div class="detail-row">
            <span>身份证号</span>
            <strong>{{ profile.idCard || '未填写' }}</strong>
          </div>
        </article>
      </section>
    </section>
  </main>
</template>

<style scoped>
.profile-page {
  min-height: 100vh;
  padding: 32px;
  background: linear-gradient(180deg, #f4f8ff 0%, #edf3ff 100%);
}

.profile-shell {
  width: min(1120px, 100%);
  margin: 0 auto;
}

.profile-header {
  display: flex;
  justify-content: space-between;
  gap: 20px;
  align-items: flex-start;
  margin-bottom: 28px;
}

.eyebrow {
  margin: 0 0 10px;
  font-size: 13px;
  letter-spacing: 0.22em;
  text-transform: uppercase;
  color: #4d77ff;
}

.profile-header h1 {
  margin: 0;
  font-size: 36px;
  color: #17243a;
}

.subtitle {
  max-width: 620px;
  margin: 14px 0 0;
  color: #5d6d88;
  line-height: 1.7;
}

.header-actions {
  display: flex;
  gap: 14px;
  align-items: center;
}

.welcome-card,
.profile-card,
.state-card {
  border-radius: 22px;
  background: #fff;
  box-shadow: 0 18px 40px rgba(26, 49, 94, 0.08);
}

.welcome-card {
  padding: 16px 20px;
  min-width: 170px;
}

.welcome-card span {
  display: block;
  color: #7a88a0;
  font-size: 13px;
}

.welcome-card strong {
  display: block;
  margin-top: 8px;
  color: #1a2640;
  font-size: 18px;
}

.logout-btn {
  border: none;
  border-radius: 14px;
  padding: 14px 18px;
  background: #1b2842;
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}

.state-card {
  padding: 28px;
  color: #31415d;
}

.state-card.error {
  color: #d73c4f;
}

.profile-grid {
  display: grid;
  grid-template-columns: 0.8fr 1.2fr;
  gap: 24px;
}

.profile-card {
  padding: 28px;
}

.summary-card {
  display: flex;
  flex-direction: column;
  justify-content: center;
  background: linear-gradient(180deg, #ffffff 0%, #f6f9ff 100%);
}

.summary-label {
  margin: 0;
  color: #72819a;
  font-size: 14px;
}

.summary-card h2 {
  margin: 10px 0 0;
  font-size: 38px;
  color: #142239;
}

.summary-name {
  margin: 12px 0 0;
  font-size: 22px;
  font-weight: 600;
  color: #2a3c59;
}

.summary-status {
  margin-top: 18px;
  width: fit-content;
  padding: 8px 14px;
  border-radius: 999px;
  background: #e8f0ff;
  color: #2c57d2;
  font-size: 13px;
  font-weight: 600;
}

.detail-card {
  display: grid;
  gap: 16px;
}

.detail-row {
  display: flex;
  justify-content: space-between;
  gap: 24px;
  padding-bottom: 14px;
  border-bottom: 1px solid #eef2f7;
}

.detail-row:last-child {
  border-bottom: none;
  padding-bottom: 0;
}

.detail-row span {
  color: #7a88a0;
}

.detail-row strong {
  color: #182337;
  font-weight: 600;
  text-align: right;
}

@media (max-width: 900px) {
  .profile-page {
    padding: 20px;
  }

  .profile-header {
    flex-direction: column;
  }

  .header-actions {
    width: 100%;
    flex-direction: column;
    align-items: stretch;
  }

  .profile-grid {
    grid-template-columns: 1fr;
  }
}
</style>
