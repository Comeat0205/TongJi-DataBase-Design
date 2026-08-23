<script setup lang="ts">
import { useRoute } from 'vue-router'
import type { NavItem } from '@/config/nav'
import { isNavItemActive } from '@/config/nav'

defineProps<{
  items: NavItem[]
  title: string
}>()

const route = useRoute()

function linkActive(item: NavItem) {
  return isNavItemActive(item, route.path)
}
</script>

<template>
  <nav class="app-nav">
    <div class="brand">
      <span class="brand-mark">TJ</span>
      <div>
        <strong>TJ-GYM</strong>
        <small>{{ title }}</small>
      </div>
    </div>
    <ul class="nav-list">
      <li v-for="item in items" :key="item.path">
        <RouterLink :to="item.path" class="nav-link" :class="{ active: linkActive(item) }">
          {{ item.label }}
        </RouterLink>
      </li>
    </ul>
  </nav>
</template>

<style scoped>
.app-nav {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 24px 16px;
  background: var(--tj-sidebar-bg);
  color: var(--tj-sidebar-text);
}

.brand {
  display: flex;
  gap: 12px;
  align-items: center;
  padding: 0 8px 24px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  margin-bottom: 20px;
}

.brand-mark {
  display: grid;
  place-items: center;
  width: 40px;
  height: 40px;
  border-radius: 12px;
  background: linear-gradient(135deg, #285cff, #5d8dff);
  color: #fff;
  font-weight: 700;
}

.brand strong {
  display: block;
  color: #fff;
  font-size: 16px;
}

.brand small {
  color: rgba(255, 255, 255, 0.55);
  font-size: 12px;
}

.nav-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: grid;
  gap: 6px;
  overflow-y: auto;
}

.nav-link {
  display: block;
  padding: 12px 14px;
  border-radius: 12px;
  color: var(--tj-sidebar-text);
  font-size: 14px;
  font-weight: 500;
  transition: background 0.2s ease, color 0.2s ease;
}

.nav-link:hover {
  background: rgba(255, 255, 255, 0.06);
  color: #fff;
}

.nav-link.active {
  background: var(--tj-sidebar-active);
  color: #fff;
}
</style>
