<script setup lang="ts">
import { nextTick, onActivated, onBeforeUnmount, onDeactivated, onMounted, ref, watch } from 'vue'

const props = defineProps<{ disabled: boolean }>()
const emit = defineEmits<{ load: [] }>()
const sentinel = ref<HTMLElement | null>(null)
let observer: IntersectionObserver | null = null
let active = false

async function checkVisibility() {
  await nextTick()
  const el = sentinel.value
  if (!active || props.disabled || !el || !el.getClientRects().length) return
  const rect = el.getBoundingClientRect()
  const root = el.closest('[data-scroll-container]')?.getBoundingClientRect()
  const top = root?.top ?? 0
  const bottom = root?.bottom ?? window.innerHeight
  if (rect.height > 0 && rect.bottom >= top && rect.top <= bottom + 160) emit('load')
}

function start() {
  if (active || !sentinel.value) return
  active = true
  observer = new IntersectionObserver(() => { void checkVisibility() }, {
    root: sentinel.value.closest('[data-scroll-container]'),
    rootMargin: '0px 0px 160px 0px',
  })
  observer.observe(sentinel.value)
  void checkVisibility()
}

function stop() {
  active = false
  observer?.disconnect()
  observer = null
}

// After each page renders, fill short viewports even if the marker never exits them.
watch(() => props.disabled, () => { void checkVisibility() }, { flush: 'post' })
onMounted(start)
onActivated(start)
onDeactivated(stop)
onBeforeUnmount(stop)
</script>

<template>
  <div ref="sentinel" class="infinite-load-trigger" aria-hidden="true" />
</template>

<style scoped>
.infinite-load-trigger {
  height: 1px;
  width: 100%;
}
</style>
