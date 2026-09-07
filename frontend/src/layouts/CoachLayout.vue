<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import AppNav from '@/components/AppNav.vue'
import { coachNav, getPreviewCoachNav } from '@/config/nav'
import { useAuthStore } from '@/stores/auth'

const props = defineProps<{
  preview?: boolean
}>()

const router = useRouter()
const authStore = useAuthStore()

const navItems = computed(() => (props.preview ? getPreviewCoachNav() : coachNav))

function logout() {
  if (props.preview) {
    router.push('/login')
    return
  }
  authStore.clearSession()
  router.push('/login')
}
</script>

<template>
  <div class="app-layout">
    <aside class="sidebar">
      <AppNav :items="navItems" title="教练端" />
    </aside>
    <div class="main-area">
      <header class="topbar">
        <div>
          <p class="topbar-label">{{ preview ? '布局预览' : 'Coach Portal' }}</p>
          <strong>{{ authStore.session?.displayName ?? '教练端骨架预览' }}</strong>
        </div>
        <button type="button" class="logout-btn" @click="logout">
          {{ preview ? '返回登录' : '退出登录' }}
        </button>
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
  grid-template-columns: 260px 1fr;
  min-height: 100vh;
  background: var(--tj-page-bg);
}

.sidebar {
  position: sticky;
  top: 0;
  height: 100vh;
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
  padding: 20px 28px;
  background: rgba(255, 255, 255, 0.82);
  border-bottom: 1px solid #e6edf8;
}

.topbar-label {
  margin: 0 0 4px;
  font-size: 12px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: #4d77ff;
}

.topbar strong {
  color: var(--tj-text);
  font-size: 18px;
}

.logout-btn {
  border: none;
  border-radius: 12px;
  padding: 12px 16px;
  background: #1b2842;
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}

.content {
  padding: 28px;
}

@media (max-width: 960px) {
  .app-layout {
    grid-template-columns: 1fr;
  }

  .sidebar {
    position: static;
    height: auto;
  }
}
</style>
