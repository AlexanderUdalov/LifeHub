<script setup lang="ts">
import Drawer from "primevue/drawer"
import { computed } from "vue"

const props = withDefaults(defineProps<{
  visible: boolean
  maxHeight?: string
  minHeight?: string
  class?: string | string[] | Record<string, boolean>
}>(), {
  maxHeight: "var(--ds-drawer-max-height)"
})

const emit = defineEmits<{
  (e: "update:visible", value: boolean): void
}>()

const drawerStyle = computed(() => ({
  height: "auto",
  maxHeight: props.maxHeight,
  minHeight: props.minHeight
}))
</script>

<template>
  <Drawer position="bottom" :visible="visible" :style="drawerStyle" class="ds-bottom-drawer" :class="props.class"
    @update:visible="emit('update:visible', $event)">
    <template v-if="$slots.header" #header>
      <slot name="header" />
    </template>
    <slot />
    <template v-if="$slots.footer" #footer>
      <slot name="footer" />
    </template>
  </Drawer>
</template>
