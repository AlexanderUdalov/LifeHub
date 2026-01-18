<script setup lang="ts">
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Editor from 'primevue/editor'
import DatePicker from 'primevue/datepicker'
import Checkbox from 'primevue/checkbox'
import { computed, ref, watch } from 'vue'
import type { TaskItem } from '@/models/TaskItem'
import type { GoalItem } from '@/models/GoalItem'
import { format, differenceInDays, isToday, isPast } from 'date-fns'

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

const isCompleted = ref(false);
watch(isCompleted, (val) => {
    localTask.value.completeDate = val ? new Date() : undefined;
});

const dueDateText = computed(() => {
    if (!localTask.value.dueDate) {
        return 'дата и повтор'
    }

    const date = new Date(localTask.value.dueDate)
    const base = format(date, 'dd.MM.yyyy')

    if (isToday(date)) {
        return `${base}, сегодня`
    }

    const diff = differenceInDays(date, new Date())

    if (diff < 0) {
        return `${base}, просрочено ${Math.abs(diff)} дн.`
    }

    return `${base}, через ${diff} дн.`
})

const dueDateClass = computed(() => ({
    'due-overdue': localTask.value.dueDate && isPast(new Date(localTask.value.dueDate)),
    'due-empty': !localTask.value.dueDate
}))

function save() {
    if (!canSave.value) return
    emit('save', localTask.value)
    emit('close')
}
</script>

<template>
    <Dialog modal :visible="true" :closable="false" style="width: 90vw; max-width: 400px" class="task-dialog"
        @hide="emit('close')">
        <template #header>
            <div class="form-field">
                <Checkbox v-if="isEdit" v-model="isCompleted"/>
                <InputText id="title" v-model="localTask.title" placeholder="New task" size="large" fluid />
            </div>

        </template>

        <div class="form-field">
            <!-- <div class="due-date-row" :class="dueDateClass" @click="duePanel?.toggle($event)"> -->
            <div class="due-date-row" :class="dueDateClass">
                <i class="pi pi-calendar" />
                <span>{{ dueDateText }}</span>
            </div>
            <!-- 
            <OverlayPanel ref="duePanel">
                <DatePicker v-model="localTask.dueDate" inline dateFormat="dd.mm.yy" />
                <Divider />
                <Dropdown v-model="localTask.repeat" :options="repeatOptions" placeholder="Повтор" />
            </OverlayPanel> -->
        </div>

        <div class="form-field">
            <Editor id="description" v-model="localTask.description" placeholder="Description"
                editorStyle="height: 200px">
                <template v-slot:toolbar>
                    <span class="ql-formats">
                        <button v-tooltip.bottom="'Bold'" class="ql-bold"></button>
                        <button v-tooltip.bottom="'Italic'" class="ql-italic"></button>
                        <button v-tooltip.bottom="'Underline'" class="ql-underline"></button>
                    </span>
                    <span class="ql-formats">
                        <button class="ql-list" value="ordered"></button>
                        <button class="ql-list" value="bullet"></button>
                    </span>
                </template>
            </Editor>
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
:deep(.p-inputtext) {
    border: none;
    box-shadow: none;
}
</style>
