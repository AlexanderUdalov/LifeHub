<script setup lang="ts">
import Button from "primevue/button"
import { computed, useAttrs } from "vue"

const props = withDefaults(defineProps<{
  dsVariant?: "primary" | "secondary" | "text" | "outlined" | "danger"
  dsSize?: "small" | "normal"
  rounded?: boolean
}>(), {
  dsVariant: "primary",
  dsSize: "normal",
  rounded: false
})

const attrs = useAttrs()

const variantAttrs = computed(() => {
  if (props.dsVariant === "secondary") return { severity: "secondary" }
  if (props.dsVariant === "text") return { variant: "text", severity: "secondary" }
  if (props.dsVariant === "outlined") return { variant: "outlined", severity: "secondary" }
  if (props.dsVariant === "danger") return { severity: "danger" }
  return {}
})

const mergedAttrs = computed(() => ({
  ...attrs,
  ...variantAttrs.value
}))
</script>

<template>
  <Button v-bind="mergedAttrs" :size="dsSize === 'small' ? 'small' : undefined" :rounded="rounded">
    <slot />
  </Button>
</template>
