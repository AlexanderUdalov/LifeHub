<script setup lang="ts">
import Dialog from "primevue/dialog"
import Drawer from "primevue/drawer"
import { computed, onBeforeUnmount, onMounted, ref } from "vue"

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

const desktopQuery = "(min-width: 900px)"
const isDesktop = ref(false)
let mediaQuery: MediaQueryList | null = null

function updateDesktopState() {
  isDesktop.value = mediaQuery?.matches ?? false
}

onMounted(() => {
  mediaQuery = window.matchMedia(desktopQuery)
  updateDesktopState()
  mediaQuery.addEventListener("change", updateDesktopState)
})

onBeforeUnmount(() => {
  mediaQuery?.removeEventListener("change", updateDesktopState)
})

const drawerStyle = computed(() => ({
  height: "auto",
  maxHeight: props.maxHeight,
  minHeight: props.minHeight
}))

const dialogStyle = computed(() => ({
  width: "min(42rem, calc(100vw - 2rem))",
  maxHeight: props.maxHeight,
  minHeight: props.minHeight
}))
</script>

<template>
  <Dialog
    v-if="isDesktop"
    :visible="visible"
    :style="dialogStyle"
    modal
    dismissable-mask
    class="ds-desktop-dialog"
    :class="props.class"
    @update:visible="emit('update:visible', $event)"
  >
    <template v-if="$slots.header" #header>
      <slot name="header" />
    </template>
    <slot />
    <template v-if="$slots.footer" #footer>
      <slot name="footer" />
    </template>
  </Dialog>

  <Drawer v-else position="bottom" :visible="visible" :style="drawerStyle" class="ds-bottom-drawer" :class="props.class"
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
