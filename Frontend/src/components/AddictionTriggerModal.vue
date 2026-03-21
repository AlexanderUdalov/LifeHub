<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import Drawer from 'primevue/drawer'
import Button from 'primevue/button'
import Carousel from 'primevue/carousel'
import { useI18n } from 'vue-i18n'

defineProps<{
  addictionTitle: string
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const { t } = useI18n()

const visible = ref(true)
watch(visible, (v) => {
  if (!v) emit('close')
})

const slides = computed(() => [
  { icon: 'pi pi-clock', text: t('addictions.triggerDrawer.slide1') },
  { icon: 'pi pi-chart-line', text: t('addictions.triggerDrawer.slide2') },
  { icon: 'pi pi-bolt', text: t('addictions.triggerDrawer.slide3') },
  { icon: 'pi pi-star', text: t('addictions.triggerDrawer.slide4') },
  { icon: 'pi pi-heart', text: t('addictions.triggerDrawer.slide5') }
])
</script>

<template>
  <Drawer
    v-model:visible="visible"
    position="bottom"
    class="addiction-drawer"
    style="height: auto; max-height: 85vh"
  >
    <template #header>
      <div class="trigger-drawer-title-row">
        <i class="pi pi-bolt" style="color: var(--p-warning-color)" />
        <span>{{ t('addictions.triggerDrawer.title') }}</span>
      </div>
    </template>

    <p class="trigger-subtitle">
      {{ t('addictions.triggerDrawer.subtitle') }}
    </p>

    <Carousel :value="slides" :numVisible="1" :numScroll="1" circular :autoplayInterval="5000">
      <template #item="{ data }">
        <div class="trigger-slide">
          <i :class="data.icon" class="trigger-slide-icon" />
          <p class="trigger-slide-text">{{ data.text }}</p>
        </div>
      </template>
    </Carousel>

    <template #footer>
      <div class="addiction-drawer-actions" style="justify-content: center">
        <Button :label="t('addictions.triggerDrawer.dismiss')" icon="pi pi-check" @click="visible = false" />
      </div>
    </template>
  </Drawer>
</template>

<style>
.trigger-drawer-title-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 1.1rem;
}

.trigger-subtitle {
  color: var(--p-text-muted-color);
  font-size: 0.95rem;
  margin-bottom: 16px;
  line-height: 1.4;
}

.trigger-slide {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 1.5rem 1rem;
  text-align: center;
  gap: 12px;
}

.trigger-slide-icon {
  font-size: 2rem;
  color: var(--p-warning-color);
}

.trigger-slide-text {
  font-size: 1.05rem;
  line-height: 1.5;
  max-width: 300px;
}
</style>
