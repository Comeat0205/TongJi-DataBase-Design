<script setup lang="ts">
defineProps<{
  message?: string
}>()
</script>

<template>
  <section class="placeholder-panel">
    <p class="message">{{ message ?? '该功能页面开发中，请稍后再试。' }}</p>
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

.message {
  margin: 0;
  color: var(--tj-text);
  line-height: 1.7;
}
</style>
