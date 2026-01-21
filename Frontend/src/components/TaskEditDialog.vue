<script setup lang="ts">
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Editor from 'primevue/editor'
import DatePicker from 'primevue/datepicker'
import Checkbox from 'primevue/checkbox'
import { computed, ref, watch } from 'vue'
import type { GoalItem } from '@/models/GoalItem'
import { format, differenceInDays, isToday, isPast } from 'date-fns'
import type { CreateTaskRequest, TaskDTO, UpdateTaskRequest } from '@/api/TasksAPI'

const props = defineProps<{
    task: TaskDTO | null
    goals?: GoalItem[]
}>()

const emit = defineEmits<{
    (e: 'close'): void
    (e: 'create', task: CreateTaskRequest): void
    (e: 'update', task: UpdateTaskRequest, id: string): void
}>()

const localTask = ref<TaskDTO>({
    id: props.task?.id ?? '',
    title: props.task?.title ?? '',
    description: props.task?.description ?? null,
    dueDate: props.task?.dueDate ? props.task.dueDate : null,
    completionDate: props.task?.completionDate ? props.task.completionDate : null,
    goalId: props.task?.goalId ? props.task.goalId : null
})

const isEdit = computed(() => !!props.task)
const canSave = computed(() => localTask.value.title.trim().length > 0)

const isCompleted = ref(false);
watch(isCompleted, (val) => {
    localTask.value.completionDate = val ? new Date().toDateString() : '';
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

const descriptionModel = computed({
    get() {
        return localTask.value.description ?? "";
    },
    set(value) {
        localTask.value.description = value || null;
    }
})

function save() {
    if (!canSave.value) return
    if (isEdit.value) {
        const request: UpdateTaskRequest = {
            title: localTask.value.title,
            description: localTask.value.description,
            dueDate: localTask.value.dueDate,
            completionDate: localTask.value.completionDate,
            goalId: localTask.value.goalId,
        }

        emit('update', request, localTask.value.id)
    } else {
        const request: CreateTaskRequest = {
            title: localTask.value.title,
            description: localTask.value.description,
            dueDate: localTask.value.dueDate,
            goalId: localTask.value.goalId,
        }

        emit('create', request)
    }
    emit('close')
}
</script>

<template>
    <Dialog modal :visible="true" :closable="false" style="width: 90vw; max-width: 400px" class="task-dialog"
        @hide="emit('close')">
        <template #header>
            <div class="form-field">
                <Checkbox v-if="isEdit" v-model="isCompleted" />
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
            <Editor id="description" v-model="descriptionModel" placeholder="Description" editorStyle="height: 200px">
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
