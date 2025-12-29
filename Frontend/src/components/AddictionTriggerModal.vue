<script setup lang="ts">
import type { AddictionItem } from '@/models/AddictionItem';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Carousel from 'primevue/carousel';


const props = defineProps<{
    addiction: AddictionItem
    visible: boolean
}>();
const emit = defineEmits<{
    (e: 'close'): void
}>();

const slides = [
    { text: 'Breathe slowly for 60 seconds.' },
    { text: 'This urge will peak and fade.' },
    { text: 'Do one small physical action: stand up, wash your face.' },
    { text: 'Remember why you started.' }
]

</script>

<template>
    <Dialog :visible="visible" modal header="Pause. Breathe." class="addiction-trigger-dialog"
        @update:visible="emit('close')">
        <p class="mb-3">
            You’re not weak. Urges pass. Stay with it for a few minutes.
        </p>

        <Carousel :value="slides" :numVisible="1" :numScroll="1" circular>
            <template #item="{ data }">
                <div class="carousel-slide">
                    <p>{{ data.text }}</p>
                </div>
            </template>
        </Carousel>

        <template #footer>
            <Button label="I’m okay now" @click="emit('close')" />
        </template>
    </Dialog>
</template>

<style>
.addiction-trigger-dialog {
    width: 90vw;
    max-width: 400px;
}

.carousel-slide {
    padding: 1rem;
    text-align: center;
    font-size: 1.1rem;
}
</style>