<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { usePreferencesStore } from '@/stores/preferences'
import { defaultMobileTabs, mobilePageIds, type MobilePageId } from '@/utils/navigation'
import { navigation } from '@/constants/navigation'
const { t } = useI18n()
const preferences = usePreferencesStore()
const item = (key: string) => navigation.find((x) => x.id === key)!
</script>
<template>
  <div class="settings-page">
    <h1 class="ds-page-header">{{ t('redesign.navigation') }}</h1>
    <section class="settings-section">
      <h2>{{ t('redesign.mobileTabs') }}</h2>
      <p>{{ t('redesign.tabsHint') }}</p>
      <label
        v-for="(tab, position) in preferences.mobileTabs"
        :key="position"
        class="navigation-setting"
        ><span class="navigation-setting-icon"
          ><i aria-hidden="true" :class="item(tab).icon" /></span
        ><span
          ><small>{{ t('redesign.tabPosition', { n: position + 1 }) }}</small
          ><select
            class="native-select"
            :value="tab"
            :aria-label="t('redesign.tabPosition', { n: position + 1 })"
            @change="
              preferences.setTab(
                position,
                ($event.target as HTMLSelectElement).value as MobilePageId,
              )
            "
          >
            <option v-for="key in mobilePageIds" :key="key" :value="key">
              {{ t(item(key).label) }}
            </option>
          </select></span
        ></label
      >
      <p>{{ t('redesign.tabsMoreHint') }}</p>
    </section>
    <div class="settings-link">
      <span class="muted" role="status">{{ t('redesign.savedImmediately') }}</span
      ><button
        type="button"
        class="text-action"
        @click="preferences.mobileTabs = [...defaultMobileTabs]"
      >
        <i aria-hidden="true" class="pi pi-refresh" />{{ t('redesign.reset') }}
      </button>
    </div>
  </div>
</template>
