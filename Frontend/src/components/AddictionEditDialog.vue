<script setup lang="ts">
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import DatePicker from 'primevue/datepicker'
import { computed, ref } from 'vue'
import type { GoalItem } from '@/models/GoalItem';
import type { AddictionItem } from '@/models/AddictionItem'

const props = defineProps<{
    addiction: AddictionItem | null
    goals?: GoalItem[]
}>()

const emit = defineEmits<{
    (e: 'close'): void
    (e: 'save', addiction: AddictionItem): void
}>()

const localItem = ref<AddictionItem>({
    id: props.addiction?.id ?? 0,
    title: props.addiction?.title ?? '',
    icon: props.addiction?.icon ?? '',
    lastReset: props.addiction?.lastReset ? props.addiction.lastReset : new Date(Date.now())
})

const isEdit = computed(() => !!props.addiction)
const canSave = computed(() => localItem.value.title.trim().length > 0)

function save() {
    if (!canSave.value) return
    emit('save', localItem.value)
    emit('close')
}
</script>

<template>
    <Dialog modal :visible="true" :closable="false" :header="isEdit ? 'Edit Addiction' : 'New Addiction'"
        style="width: 90vw; max-width: 400px" @hide="emit('close')">
        <div class="form-field">
            <label for="title">Title</label>
            <InputText id="title" v-model="localItem.title" />
        </div>

        <!-- todo: dropdown -->
        <div class="form-field">
            <label for="description">Icon</label>
            <InputText id="description" v-model="localItem.icon" />
        </div>

        <div class="form-field">
            <label for="dueDate">Last Reset</label>
            <DatePicker id="dueDate" v-model="localItem.lastReset" showIcon dateFormat="yy-mm-dd" />
        </div>

        <div v-if="props.goals?.length" class="form-field">
            <label for="goal">Assign to Goal</label>
            <select id="goal" v-model.number="localItem.goalId">
                <option v-for="goal in props.goals" :key="goal.id" :value="goal.id">
                    {{ goal.title }}
                </option>
            </select>
        </div>
        <template #footer>
            <Button label="Cancel" text @click="emit('close')" />
            <Button :label="isEdit ? 'Save' : 'Create'" :disabled="!canSave" @click="save" />
        </template>
    </Dialog>
</template>

<style scoped>
.form-field {
    margin-bottom: 1rem;
    display: flex;
    flex-direction: column;
    gap: 0.25rem;
}
</style>
