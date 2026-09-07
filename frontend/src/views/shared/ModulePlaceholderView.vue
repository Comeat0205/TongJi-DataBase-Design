<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import PageHeader from '@/components/ui/PageHeader.vue'
import PlaceholderPanel from '@/components/ui/PlaceholderPanel.vue'

const route = useRoute()

const title = computed(() => String(route.meta.pageTitle ?? '功能开发中'))
const subtitle = computed(() => route.meta.pageSubtitle as string | undefined)
const owner = computed(() => route.meta.owner as string | undefined)
const features = computed(() => route.meta.features as string | undefined)
const eyebrow = computed(() => route.meta.eyebrow as string | undefined)
</script>

<template>
  <div>
    <PageHeader :title="title" :subtitle="subtitle" :eyebrow="eyebrow" />
    <PlaceholderPanel :owner="owner" :features="features" />
  </div>
</template>
