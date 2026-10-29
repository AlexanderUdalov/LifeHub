<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { navigation } from '@/constants/navigation'
import { usePreferencesStore } from '@/stores/preferences'
const { t } = useI18n()
const preferences = usePreferencesStore()
const pages = computed(() =>
  navigation.filter((x) => !preferences.mobileTabs.some((key) => key === x.id)),
)
</script>
<template>
  <div>
    <h1 class="ds-page-header">{{ t('redesign.mySpace') }}</h1>
    <div class="more-grid">
      <RouterLink v-for="page in pages" :key="page.id" :to="page.path" class="more-link"
        ><i aria-hidden="true" :class="page.icon" />{{ t(page.label)
        }}<i aria-hidden="true" class="pi pi-angle-right"
      /></RouterLink>
    </div>
    <RouterLink to="/navigation" class="text-action more-settings"
      ><i aria-hidden="true" class="pi pi-cog" />{{ t('redesign.configureNavigation') }}</RouterLink
    >
  </div>
</template>
