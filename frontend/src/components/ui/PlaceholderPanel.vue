<script setup lang="ts">
defineProps<{
  message?: string
  owner?: string
  features?: string
}>()
</script>

<template>
  <section class="placeholder-panel">
    <p class="badge">站点骨架 · 占位页</p>
    <p class="message">{{ message ?? '接口与业务逻辑开发中，当前仅用于页面流转与布局统一。' }}</p>
    <div v-if="owner || features" class="meta">
      <span v-if="owner">负责人：{{ owner }}</span>
      <span v-if="features">功能点：{{ features }}</span>
    </div>
  </section>
</template>

<style scoped>
.placeholder-panel {
  padding: 28px;
  border-radius: var(--tj-radius);
  background: linear-gradient(180deg, #ffffff 0%, #f8fbff 100%);
  border: 1px dashed #c8d6ef;
  box-shadow: var(--tj-shadow);
}

.badge {
  margin: 0 0 12px;
  display: inline-block;
  padding: 6px 12px;
  border-radius: 999px;
  background: var(--tj-primary-soft);
  color: #2c57d2;
  font-size: 13px;
  font-weight: 600;
}

.message {
  margin: 0;
  color: var(--tj-text);
  line-height: 1.7;
}

.meta {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  margin-top: 16px;
  color: var(--tj-text-muted);
  font-size: 14px;
}
</style>
