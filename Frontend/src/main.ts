import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { GesturePlugin } from '@vueuse/gesture'

import PrimeVue from 'primevue/config'
import Aura from '@primeuix/themes/aura';

import 'primeicons/primeicons.css'
import './main.css'

const pinia = createPinia()
const app = createApp(App)

app.use(pinia)
app.use(router)
app.use(GesturePlugin)
app.use(PrimeVue, {
    theme: {
        preset: Aura
    }
});

app.mount('#app')
