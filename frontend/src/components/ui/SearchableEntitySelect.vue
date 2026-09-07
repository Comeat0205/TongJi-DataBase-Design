<script setup lang="ts">
import { computed, ref, useId, watch } from 'vue'

interface EntityOption {
  id: number
  name: string
}

const props = withDefaults(
  defineProps<{
    label: string
    options: EntityOption[]
    placeholder?: string
    emptyText?: string
    disabled?: boolean
    required?: boolean
  }>(),
  {
    placeholder: '输入名称或编号搜索',
    emptyText: '没有可选项',
    disabled: false,
    required: false,
  },
)

const model = defineModel<number | undefined>()
const inputId = useId()
const listboxId = `${inputId}-listbox`
const query = ref('')
const isOpen = ref(false)
const isEditing = ref(false)
const highlightedIndex = ref(-1)

const filteredOptions = computed(() => {
  const keyword = query.value.trim().toLocaleLowerCase('zh-CN')
  const options = keyword
    ? props.options.filter(
        (option) =>
          option.name.toLocaleLowerCase('zh-CN').includes(keyword) ||
          String(option.id).includes(keyword),
      )
    : props.options

  return options.slice(0, 20)
})

const activeOptionId = computed(() => {
  if (highlightedIndex.value < 0) return undefined
  const option = filteredOptions.value[highlightedIndex.value]
  return option ? `${inputId}-option-${option.id}` : undefined
})

function optionLabel(option: EntityOption) {
  return `${option.name}（#${option.id}）`
}

function syncQueryWithModel() {
  const selected = props.options.find((option) => option.id === model.value)
  query.value = selected ? optionLabel(selected) : ''
}

function openOptions() {
  if (props.disabled) return
  isEditing.value = true
  isOpen.value = true
  highlightedIndex.value = filteredOptions.value.length > 0 ? 0 : -1
}

function handleInput(event: Event) {
  query.value = (event.target as HTMLInputElement).value
  model.value = undefined
  isOpen.value = true
  highlightedIndex.value = filteredOptions.value.length > 0 ? 0 : -1
}

function selectOption(option: EntityOption) {
  model.value = option.id
  query.value = optionLabel(option)
  isEditing.value = false
  isOpen.value = false
  highlightedIndex.value = -1
}

function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    isOpen.value = false
    highlightedIndex.value = -1
    return
  }

  if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
    event.preventDefault()
    if (!isOpen.value) {
      openOptions()
      return
    }

    const direction = event.key === 'ArrowDown' ? 1 : -1
    const optionCount = filteredOptions.value.length
    if (optionCount === 0) return
    highlightedIndex.value =
      (highlightedIndex.value + direction + optionCount) % optionCount
    return
  }

  if (event.key === 'Enter' && isOpen.value && highlightedIndex.value >= 0) {
    const option = filteredOptions.value[highlightedIndex.value]
    if (option) {
      event.preventDefault()
      selectOption(option)
    }
  }
}

function handleBlur() {
  window.setTimeout(() => {
    isOpen.value = false
    isEditing.value = false
    highlightedIndex.value = -1
    syncQueryWithModel()
  }, 100)
}

watch(
  [model, () => props.options],
  () => {
    if (!isEditing.value) syncQueryWithModel()
  },
  { immediate: true },
)
</script>

<template>
  <div class="entity-select">
    <label :for="inputId">{{ label }}</label>
    <div class="select-control">
      <input
        :id="inputId"
        :value="query"
        type="text"
        role="combobox"
        autocomplete="off"
        :placeholder="placeholder"
        :disabled="disabled"
        :required="required"
        :aria-expanded="isOpen"
        :aria-controls="listboxId"
        :aria-activedescendant="activeOptionId"
        aria-autocomplete="list"
        @focus="openOptions"
        @input="handleInput"
        @keydown="handleKeydown"
        @blur="handleBlur"
      />
      <ul v-if="isOpen" :id="listboxId" class="option-list" role="listbox">
        <li v-if="filteredOptions.length === 0" class="empty-option">{{ emptyText }}</li>
        <li
          v-for="(option, index) in filteredOptions"
          v-else
          :id="`${inputId}-option-${option.id}`"
          :key="option.id"
          class="option-item"
          :class="{ highlighted: index === highlightedIndex }"
          role="option"
          :aria-selected="option.id === model"
          @mousedown.prevent="selectOption(option)"
        >
          <span>{{ option.name }}</span>
          <small>#{{ option.id }}</small>
        </li>
      </ul>
    </div>
  </div>
</template>

<style scoped>
.entity-select { position: relative; display: grid; min-width: 220px; gap: 6px; color: #2a3c59; font-size: 14px; }
.select-control { position: relative; }
input { width: 100%; min-height: 40px; padding: 8px 11px; border: 1px solid #d7e0ef; border-radius: 9px; background: #fff; color: var(--tj-text); }
input:focus { border-color: var(--tj-primary); outline: 3px solid rgb(68 112 255 / 14%); }
input:disabled { background: #f4f6fa; color: var(--tj-text-muted); cursor: not-allowed; }
.option-list { position: absolute; z-index: 20; top: calc(100% + 6px); left: 0; right: 0; max-height: 240px; margin: 0; padding: 6px; overflow-y: auto; list-style: none; border: 1px solid #d7e0ef; border-radius: 10px; background: #fff; box-shadow: 0 12px 28px rgb(21 42 83 / 16%); }
.option-item { display: flex; justify-content: space-between; gap: 12px; padding: 9px 10px; border-radius: 7px; cursor: pointer; }
.option-item.highlighted, .option-item:hover { background: #edf2ff; color: #285cff; }
.option-item small { color: var(--tj-text-muted); }
.empty-option { padding: 10px; color: var(--tj-text-muted); }
</style>
