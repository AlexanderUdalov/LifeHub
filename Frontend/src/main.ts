import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createI18n } from 'vue-i18n'
import ru from '@/locales/ru.json'
import en from '@/locales/en.json'
import App from './App.vue'
import router from './router'
import { GesturePlugin } from '@vueuse/gesture'
import ToastService from 'primevue/toastservice';

import PrimeVue from 'primevue/config'
import ruPrime from 'primelocale/ru.json'
import enPrime from 'primelocale/en.json'
import Aura from '@primeuix/themes/aura'

import 'primeicons/primeicons.css'
import './main.css'
import { applyTheme, getStoredTheme } from '@/utils/theme'

const storedTheme = getStoredTheme()
applyTheme(storedTheme ?? 'auto')

const primeLocales = { ru: ruPrime.ru, en: enPrime.en }
const initialLocale = (() => {
    const saved = localStorage.getItem('locale')
    return saved === 'en' || saved === 'ru' ? saved : 'ru'
})()
const i18n = createI18n({
    legacy: false,
    locale: initialLocale,
    fallbackLocale: 'en',
    messages: { ru, en }
})
const pinia = createPinia()
const app = createApp(App)

app.use(i18n)
app.use(pinia)
app.use(router)
app.use(ToastService);
app.use(GesturePlugin)
app.use(PrimeVue, {
    theme: {
        preset: Aura,
        options: { darkModeSelector: '.p-dark' }
    },
    locale: primeLocales[initialLocale]
});

app.mount('#app')
