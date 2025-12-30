<script setup lang="ts">
import type { AddictionItem } from '@/models/AddictionItem';
import Card from 'primevue/card';
import ProgressBar from 'primevue/progressbar'
import Button from 'primevue/button';
import { useAddictionProgress } from '@/composables/useAddictionProgress'
import { toRef } from 'vue';
import { useRouter } from 'vue-router';
import SwipeableCard from './SwipeableCard.vue';

const props = defineProps<{ addiction: AddictionItem }>()
const emit = defineEmits<{
    (e: 'trigger', addiction: AddictionItem): void,
    (e: 'reset', addiction: AddictionItem): void
}>()

const {
    elapsedHours,
    elapsedText,
    progressPercent,
    currentStage,
    nextStageText
} = useAddictionProgress(toRef(props.addiction))

const router = useRouter()

const open = () => {
    router.push({
        name: 'addiction-details',
        params: { id: props.addiction.id }
    })
}

const edit = () => { }
const remove = () => { }
const archive = () => { }

</script>

<template>
    <SwipeableCard :left-actions="[
        {
            icon: 'pi pi-pencil',
            bg: 'var(--blue-500)',
            onClick: () => edit()
        }
    ]" :right-actions="[
        {
            icon: 'pi pi-trash',
            bg: 'var(--red-500)',
            onClick: () => remove()
        },
        {
            icon: 'pi pi-flag',
            bg: 'var(--orange-500)',
            onClick: () => archive()
        }
    ]" @open="open()">
        <Card class="addiction-card">
            <template #content>
                <div class="card-header">
                    <div class="icon-wrapper">
                        <i class="pi icon" :class="addiction.icon" />
                    </div>
                    <div class="text-wrapper">
                        <div class="title">
                            {{ addiction.title }}
                        </div>
                        <div class="elapsed">
                            {{ elapsedText }}
                        </div>
                    </div>
                    <div class="control-wrapper">
                        <Button label="Trigger" icon="pi pi-bolt" severity="danger"
                            @click="emit('trigger', addiction)" />
                        <Button label="Reset" icon="pi pi-undo" @click="emit('reset', addiction)" />
                    </div>
                </div>

                <div class="progress-wrapper">
                    <ProgressBar :value="progressPercent">{{ nextStageText }}</ProgressBar>
                </div>
            </template>
        </Card>
    </SwipeableCard>
</template>


<style>
.addiction-card {
    margin: 12px;
}

.card-header {
    display: flex;
    gap: 12px;
    align-items: center;
}

.icon-wrapper {
    width: 40px;
    height: 40px;
    border-radius: 12px;
    background: var(--surface-200);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}

.icon {
    font-size: 20px;
}

.text-wrapper {
    display: flex;
    flex-direction: column;
}

.title {
    font-weight: 600;
    line-height: 1.2;
}

.elapsed {
    font-size: 0.85rem;
    color: var(--text-color-secondary);
}

.progress-wrapper {
    margin-top: 12px;
}
</style>