import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createI18n } from 'vue-i18n'
import ru from '@/locales/ru.json'
import en from '@/locales/en.json'
import App from './App.vue'
import router from './router'
import { GesturePlugin } from '@vueuse/gesture'

import PrimeVue from 'primevue/config'
import ruPrime from 'primelocale/ru.json'
import enPrime from 'primelocale/en.json'
import Aura from '@primeuix/themes/aura'

import 'primeicons/primeicons.css'
import './main.css'

const primeLocales = { ru: ruPrime.ru, en: enPrime.en }
const i18n = createI18n({
    legacy: false,
    locale: 'ru',
    fallbackLocale: 'en',
    messages: { ru, en }
})
const pinia = createPinia()
const app = createApp(App)

app.use(i18n)
app.use(pinia)
app.use(router)
app.use(GesturePlugin)
app.use(PrimeVue, {
    theme: {
        preset: Aura
    },
    locale: primeLocales[i18n.global.locale.value]
});

app.mount('#app')
