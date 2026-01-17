<script setup lang="ts">
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import DatePicker from 'primevue/datepicker'
import { computed, ref } from 'vue'
import type { TaskItem } from '@/models/TaskItem'
import type { GoalItem } from '@/models/GoalItem';

const props = defineProps<{
    task: TaskItem | null
    goals?: GoalItem[]
}>()

const emit = defineEmits<{
    (e: 'close'): void
    (e: 'save', task: TaskItem): void
}>()

const localTask = ref<TaskItem>({
    id: props.task?.id ?? 0,
    title: props.task?.title ?? '',
    description: props.task?.description ?? '',
    dueDate: props.task?.dueDate ? props.task.dueDate : undefined,
    completeDate: props.task?.completeDate ? props.task.completeDate : undefined,
})

const isEdit = computed(() => !!props.task)
const canSave = computed(() => localTask.value.title.trim().length > 0)

function save() {
    if (!canSave.value) return
    emit('save', localTask.value)
    emit('close')
}
</script>

<template>
    <Dialog modal :visible="true" :closable="false" :header="isEdit ? 'Edit Task' : 'New Task'"
        style="width: 90vw; max-width: 400px" @hide="emit('close')">
        <div class="form-field">
            <label for="title">Title</label>
            <InputText id="title" v-model="localTask.title" />
        </div>

        <div class="form-field">
            <label for="description">Description</label>
            <Textarea id="description" v-model="localTask.description" rows="3" />
        </div>

        <div class="form-field">
            <label for="dueDate">Due Date</label>
            <DatePicker id="dueDate" v-model="localTask.dueDate" showIcon dateFormat="yy-mm-dd" />
        </div>

        <div v-if="props.goals?.length" class="form-field">
            <label for="goal">Assign to Goal</label>
            <select id="goal" v-model.number="localTask.goalId">
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
