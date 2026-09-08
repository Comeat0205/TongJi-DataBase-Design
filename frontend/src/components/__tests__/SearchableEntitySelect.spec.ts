import { describe, expect, it } from 'vitest'

import { mount } from '@vue/test-utils'
import SearchableEntitySelect from '../ui/SearchableEntitySelect.vue'

describe('SearchableEntitySelect', () => {
  const options = [
    { id: 1001, name: '一号跑步机' },
    { id: 1002, name: '二号动感单车' },
  ]

  it('filters by name and emits the selected entity id', async () => {
    const wrapper = mount(SearchableEntitySelect, {
      props: {
        label: '器材',
        options,
      },
    })

    const input = wrapper.get('input')
    await input.trigger('focus')
    await input.setValue('跑步')

    expect(wrapper.text()).toContain('一号跑步机')
    expect(wrapper.text()).not.toContain('二号动感单车')

    await wrapper.get('[role="option"]').trigger('mousedown')

    expect(wrapper.emitted('update:modelValue')).toContainEqual([1001])
    expect(input.element.value).toBe('一号跑步机（#1001）')
  })

  it('supports keyboard selection after filtering by id', async () => {
    const wrapper = mount(SearchableEntitySelect, {
      props: {
        label: '器材',
        options,
      },
    })

    const input = wrapper.get('input')
    await input.trigger('focus')
    await input.setValue('1002')
    await input.trigger('keydown', { key: 'Enter' })

    expect(wrapper.emitted('update:modelValue')).toContainEqual([1002])
  })
})
