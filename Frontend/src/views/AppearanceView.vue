<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { usePreferencesStore } from '@/stores/preferences'
import { accents, backgrounds } from '@/utils/appearance'
const { t } = useI18n()
const preferences = usePreferencesStore()
function setCustomAccent(event: Event) {
  preferences.customAccent = (event.target as HTMLInputElement).value
  preferences.accent = 'custom'
}
</script>
<template>
  <div class="settings-page">
    <h1 class="ds-page-header">{{ t('redesign.appearance') }}</h1>
    <section class="settings-section settings-link">
      <div>
        <h2>{{ t('redesign.navigation') }}</h2>
        <p>{{ t('redesign.navigationHint') }}</p>
      </div>
      <RouterLink to="/navigation" class="text-action"
        >{{ t('redesign.configure') }} <i aria-hidden="true" class="pi pi-angle-right"
      /></RouterLink>
    </section>
    <section class="settings-section">
      <h2>{{ t('redesign.design') }}</h2>
      <div class="setting-options two">
        <button
          v-for="value in ['focus', 'rhythm'] as const"
          :key="value"
          type="button"
          :aria-pressed="preferences.design === value"
          @click="preferences.design = value"
        >
          {{ t('redesign.' + value) }}
        </button>
      </div>
    </section>
    <section class="settings-section">
      <h2>{{ t('profile-view.theme') }}</h2>
      <div class="setting-options">
        <button
          v-for="value in ['auto', 'light', 'dark'] as const"
          :key="value"
          type="button"
          :aria-pressed="preferences.theme === value"
          @click="preferences.theme = value"
        >
          <i
            aria-hidden="true"
            :class="'pi pi-' + { auto: 'desktop', light: 'sun', dark: 'moon' }[value]"
          />{{ t('redesign.' + value) }}
        </button>
      </div>
      <h2>{{ t('redesign.accent') }}</h2>
      <div class="color-options">
        <button
          v-for="value in accents"
          :key="value.id"
          type="button"
          :aria-pressed="preferences.accent === value.id"
          @click="preferences.accent = value.id"
        >
          <span class="color-swatch" :style="{ background: value[preferences.mode] }" />{{
            t('redesign.' + value.id)
          }}
        </button>
      </div>
      <label class="custom-color"
        ><input
          type="color"
          :value="preferences.customAccent"
          @input="setCustomAccent"
        /><span>{{ t('redesign.customAccent') }}</span></label
      >
    </section>
    <section class="settings-section">
      <h2>{{ t('redesign.background') }}</h2>
      <div class="color-options">
        <button
          v-for="value in backgrounds"
          :key="value.id"
          type="button"
          :aria-pressed="preferences.background === value.id"
          @click="preferences.background = value.id"
        >
          <span
            class="color-swatch background-swatch"
            :style="{ background: value[preferences.mode] }"
          />{{ t('redesign.' + value.id) }}
        </button>
      </div>
      <label class="custom-color"
        ><input
          type="color"
          :value="preferences.backgroundColor"
          @input="preferences.setCustomBackground(($event.target as HTMLInputElement).value)"
        /><span
          >{{ t('redesign.customBackground')
          }}<small>{{
            t('redesign.forTheme', { theme: t('redesign.' + preferences.mode).toLowerCase() })
          }}</small></span
        ></label
      >
      <p>{{ t('redesign.backgroundHint') }}</p>
    </section>
    <section class="settings-section settings-link">
      <div>
        <h2>{{ t('redesign.areaColors') }}</h2>
        <p>{{ t('redesign.areaColorsHint') }}</p>
      </div>
      <RouterLink to="/lifeareas" class="text-action"
        >{{ t('redesign.configure') }} <i aria-hidden="true" class="pi pi-angle-right"
      /></RouterLink>
    </section>
  </div>
</template>
