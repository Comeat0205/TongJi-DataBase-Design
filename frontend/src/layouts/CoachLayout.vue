<script setup lang="ts">
import { useRouter } from 'vue-router'
import AppNav from '@/components/AppNav.vue'
import { coachNav } from '@/config/nav'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

function logout() {
  authStore.clearSession()
  router.push('/login')
}
</script>

<template>
  <div class="app-layout">
    <aside class="sidebar">
      <AppNav :items="coachNav" title="教练端" />
    </aside>
    <div class="main-area">
      <header class="topbar">
        <div>
          <p class="topbar-label">Coach Portal</p>
          <strong>{{ authStore.session?.displayName ?? '教练' }}</strong>
        </div>
        <button type="button" class="logout-btn" @click="logout">退出登录</button>
      </header>
      <main class="content">
        <RouterView />
      </main>
    </div>
  </div>
</template>

<style scoped>
.app-layout {
  display: grid;
  grid-template-columns: 248px 1fr;
  gap: 0;
  min-height: 100vh;
  --tj-sidebar-active-bg: var(--tj-member-page-bg);
  background: var(--tj-member-page-bg);
  padding: 14px;
  box-sizing: border-box;
}

.sidebar {
  position: sticky;
  top: 14px;
  height: calc(100vh - 28px);
  border-radius: var(--tj-sidebar-radius);
  overflow: hidden;
  box-shadow: 0 18px 40px rgba(6, 27, 53, 0.22);
}

.main-area {
  min-width: 0;
  display: flex;
  flex-direction: column;
}

.topbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  padding: 18px 28px;
  background: var(--tj-card-bg);
  border-bottom: 1px solid #e6edf8;
}

.topbar-label {
  margin: 0 0 4px;
  color: var(--tj-text-muted);
  font-size: 12px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.topbar strong {
  color: var(--tj-text);
  font-size: 16px;
}

.logout-btn {
  border: 1px solid #d7e0ef;
  border-radius: 10px;
  padding: 8px 14px;
  background: #fff;
  color: #2a3c59;
  font-weight: 600;
  cursor: pointer;
}

.content {
  padding: 24px 28px 40px;
}

@media (max-width: 960px) {
  .app-layout {
    grid-template-columns: 1fr;
    padding: 12px;
  }

  .sidebar {
    position: static;
    height: auto;
    border-radius: 22px;
  }
}
</style>
