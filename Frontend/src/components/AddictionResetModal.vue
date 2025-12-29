<script setup lang="ts">
import { ref } from 'vue'
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import Textarea from 'primevue/textarea'
import type { AddictionItem } from '@/models/AddictionItem';

const props = defineProps<{
    addiction: AddictionItem
    visible: boolean
}>();
const emit = defineEmits<{
    (e: 'confirm', payload: { addiction: AddictionItem; note: string }): void
    (e: 'close'): void
}>();

const note = ref('')

function confirmReset() {
    emit('confirm', {
        addiction: props.addiction,
        note: note.value
    })
    note.value = ''
}

function close() {
    emit('close')
    note.value = ''
}
</script>

<template>
    <Dialog :visible="visible" modal header="Reset progress?" class="addiction-reset-dialog" @update:visible="close">
        <p class="mb-3">
            This will reset your progress for <b>{{ addiction.title }}</b>.
        </p>

        <Textarea v-model="note" rows="4" autoResize placeholder="What happened? What can you do differently next time?"
            class="w-full" />

        <template #footer>
            <Button label="Cancel" severity="secondary" text @click="close" />
            <Button label="Reset" severity="danger" @click="confirmReset" />
        </template>
    </Dialog>
</template>

<style>
.addiction-reset-dialog {
    width: 90vw;
    max-width: 400px;
}
</style>