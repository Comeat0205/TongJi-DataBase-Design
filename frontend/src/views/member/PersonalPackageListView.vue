<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ApiError } from '@/api/http'
import {
  getMemberPersonalPackages,
  type PersonalPackage,
} from '@/api/personal-packages'
import PageHeader from '@/components/ui/PageHeader.vue'
import StateCard from '@/components/ui/StateCard.vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const packages = ref<PersonalPackage[]>([])
const loading = ref(true)
const errorMessage = ref('')

const memberId = computed(() =>
  authStore.session?.userType === 'member' ? authStore.session.userId : 1,
)
const usableCount = computed(() => packages.value.filter((item) => item.isUsable).length)

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('zh-CN')
}

async function loadPackages() {
  loading.value = true
  errorMessage.value = ''

  try {
    packages.value = await getMemberPersonalPackages(memberId.value)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '私教课包加载失败，请稍后重试。'
  } finally {
    loading.value = false
  }
}

onMounted(loadPackages)
</script>

<template>
  <div>
    <PageHeader
      eyebrow="Personal Training"
      title="我的私教课包"
      subtitle="查看已购买课包的课程、教练、剩余次数与有效期。"
    >
      <template #actions>
        <button type="button" class="refresh-btn" :disabled="loading" @click="loadPackages">
          刷新
        </button>
      </template>
    </PageHeader>

    <StateCard v-if="loading" message="私教课包加载中..." />
    <StateCard v-else-if="errorMessage" :message="errorMessage" type="error" />

    <template v-else>
      <section class="summary-strip">
        <div>
          <span>课包总数</span>
          <strong>{{ packages.length }}</strong>
        </div>
        <div>
          <span>当前可预约</span>
          <strong>{{ usableCount }}</strong>
        </div>
      </section>

      <StateCard v-if="packages.length === 0" message="当前没有私教课包，请联系前台购买或录入测试课包。" />

      <section v-else class="package-grid">
        <article v-for="item in packages" :key="item.packageId" class="package-card">
          <div class="card-head">
            <div>
              <p>课包 #{{ item.packageId }}</p>
              <h2>{{ item.courseName }}</h2>
            </div>
            <span class="status" :class="{ disabled: !item.isUsable }">
              {{ item.isUsable ? '可预约' : '不可用' }}
            </span>
          </div>

          <p class="description">{{ item.courseDescription || '暂无课程简介' }}</p>

          <dl>
            <div>
              <dt>负责教练</dt>
              <dd>{{ item.coachName }}</dd>
            </div>
            <div>
              <dt>剩余次数</dt>
              <dd class="sessions">{{ item.remainingSessions }} / {{ item.totalSessions }}</dd>
            </div>
            <div>
              <dt>有效期至</dt>
              <dd>{{ formatDate(item.expireDate) }}</dd>
            </div>
            <div>
              <dt>数据库状态</dt>
              <dd>{{ item.packageStatus }}</dd>
            </div>
          </dl>
        </article>
      </section>
    </template>
  </div>
</template>

<style scoped>
.refresh-btn {
  border: 0;
  border-radius: 10px;
  padding: 10px 18px;
  background: #315fe8;
  color: white;
  cursor: pointer;
}

.refresh-btn:disabled {
  opacity: 0.55;
  cursor: wait;
}

.summary-strip {
  display: flex;
  gap: 16px;
  margin-bottom: 20px;
}

.summary-strip div {
  min-width: 150px;
  padding: 16px 20px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.summary-strip span {
  display: block;
  color: var(--tj-text-muted);
  font-size: 13px;
}

.summary-strip strong {
  display: block;
  margin-top: 6px;
  color: var(--tj-text);
  font-size: 26px;
}

.package-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 18px;
}

.package-card {
  padding: 22px;
  border-radius: var(--tj-radius);
  background: var(--tj-card-bg);
  box-shadow: var(--tj-shadow);
}

.card-head {
  display: flex;
  justify-content: space-between;
  gap: 16px;
}

.card-head p,
.description {
  margin: 0;
  color: var(--tj-text-muted);
}

.card-head h2 {
  margin: 6px 0 0;
  color: var(--tj-text);
  font-size: 21px;
}

.status {
  height: fit-content;
  padding: 5px 10px;
  border-radius: 999px;
  background: #e9f8ef;
  color: #187342;
  font-size: 12px;
  white-space: nowrap;
}

.status.disabled {
  background: #f5eeee;
  color: #a13a3a;
}

.description {
  min-height: 42px;
  margin-top: 16px;
  line-height: 1.6;
}

dl {
  margin: 18px 0 0;
}

dl div {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding: 11px 0;
  border-top: 1px solid var(--tj-border);
}

dt {
  color: var(--tj-text-muted);
}

dd {
  margin: 0;
  color: var(--tj-text);
  font-weight: 600;
}

.sessions {
  color: #315fe8;
}

@media (max-width: 640px) {
  .summary-strip {
    display: grid;
    grid-template-columns: 1fr 1fr;
  }

  .summary-strip div {
    min-width: 0;
  }
}
</style>
