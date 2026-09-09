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
  padding: 28px 0 24px;
  background: var(--tj-sidebar-bg);
  color: var(--tj-sidebar-text);
}

.brand {
  display: flex;
  gap: 12px;
  align-items: center;
  padding: 0 22px 24px;
  margin: 0 12px 18px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
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
  padding: 18px 0 18px 14px;
  margin: 0;
  display: grid;
  gap: 4px;
  overflow-y: auto;
  overflow-x: hidden;
}

.nav-link {
  position: relative;
  display: block;
  padding: 13px 18px;
  border-radius: 20px 0 0 20px;
  color: var(--tj-sidebar-text);
  font-size: 14px;
  font-weight: 500;
  /* 背景不过渡：否则 active 切换时底色渐变、凹角伪元素瞬切，会闪一帧“无圆角” */
  transition: color 0.15s ease;
}

.nav-link:hover:not(.active) {
  background: rgba(255, 255, 255, 0.06);
  color: #fff;
}

.nav-link.active {
  background: var(--tj-sidebar-active-bg);
  color: var(--tj-sidebar-active-text);
  font-weight: 700;
  z-index: 1;
}

/* 伪元素常驻，靠透明度显隐，避免 class 切换时“突然插入/销毁” */
.nav-link::before,
.nav-link::after {
  content: '';
  position: absolute;
  right: 0;
  width: 18px;
  height: 18px;
  background: transparent;
  pointer-events: none;
  opacity: 0;
}

.nav-link::before {
  top: -18px;
  border-bottom-right-radius: 13px;
  box-shadow: 6px 6px 0 0 var(--tj-sidebar-active-bg);
}

.nav-link::after {
  bottom: -18px;
  border-top-right-radius: 13px;
  box-shadow: 6px -6px 0 0 var(--tj-sidebar-active-bg);
}

.nav-link.active::before,
.nav-link.active::after {
  opacity: 1;
}
</style>
