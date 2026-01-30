<script setup lang="ts">
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import FloatLabel from 'primevue/floatlabel'
import Textarea from 'primevue/textarea'
import DatePicker from 'primevue/datepicker'
import Checkbox from 'primevue/checkbox'
import Message from 'primevue/message'
import { computed, ref } from 'vue'
import { createTask, deleteTask, updateTask, type CreateTaskRequest, type TaskDTO, type UpdateTaskRequest } from '@/api/TasksAPI'
import { useI18n } from 'vue-i18n'

const { t } = useI18n();
const props = defineProps<{
    task: TaskDTO | null
}>()

import { useApiError } from "@/composables/useApiError";
const apiError = useApiError();

const emit = defineEmits<{
    (e: 'close', update: boolean): void
}>()

const localTask = ref<TaskDTO>({
    id: props.task?.id ?? '',
    title: props.task?.title ?? '',
    description: props.task?.description ?? null,
    dueDate: props.task?.dueDate ? props.task.dueDate : null,
    completionDate: props.task?.completionDate ? props.task.completionDate : null,
    goalId: props.task?.goalId ? props.task.goalId : null
})

const dueDateAsDate = computed<Date | null>({
    get() {
        return localTask.value.dueDate
            ? new Date(localTask.value.dueDate)
            : null
    },
    set(value) {
        localTask.value.dueDate = value?.toISOString() ?? null;
    }
})

const isEdit = computed(() => !!props.task)
const canSave = computed(() => localTask.value.title.trim().length > 0)

const completedModel = computed({
    get() {
        return localTask.value.completionDate != null;
    },
    set(value) {
        localTask.value.completionDate = value ? new Date().toISOString() : null;
    }
})

const descriptionModel = computed({
    get() {
        return localTask.value.description ?? "";
    },
    set(value) {
        localTask.value.description = value || null;
    }
})

const isSaveLoading = ref<boolean>(false);
const isDeleteLoading = ref<boolean>(false);
const errorText = ref<string>("");

async function onSave() {
    if (!canSave.value) return
    try {
        errorText.value = "";
        isSaveLoading.value = true;
        if (isEdit.value) {
            const request: UpdateTaskRequest = {
                title: localTask.value.title,
                description: localTask.value.description,
                dueDate: localTask.value.dueDate,
                completionDate: localTask.value.completionDate,
                goalId: localTask.value.goalId,
            }

            await updateTask(localTask.value.id, request)
        } else {
            const request: CreateTaskRequest = {
                title: localTask.value.title,
                description: localTask.value.description,
                dueDate: localTask.value.dueDate,
                goalId: localTask.value.goalId,
            }
            await createTask(request)
        }
        emit('close', true)
    }
    catch (e) {
        errorText.value = apiError.resolveMessage(e)
    }
    finally {
        isSaveLoading.value = false;
    }
}

async function onDelete() {
    try {
        errorText.value = "";
        isDeleteLoading.value = true;
        await deleteTask(localTask.value.id)
        emit('close', true)
    }
    catch (e) {
        errorText.value = apiError.resolveMessage(e)
    }
    finally {
        isDeleteLoading.value = false;
    }
}
</script>

<template>
    <Dialog modal :visible="true" :closable="false" :draggable="false" @hide="emit('close', false)" :pt="{
        header: { class: 'task-edit-header' },
        content: { class: 'task-edit-content' }
    }">
        <template #header>
            <Checkbox v-if="isEdit" binary v-model="completedModel" />
            <InputText id="title" class="task-edit-title" :class="{ completed: completedModel }"
                v-model="localTask.title" :placeholder="t('tasks.editdialog.newTask')" size="large" fluid />
            <Button icon="pi pi-times" severity="secondary" rounded variant="outlined" aria-label="Cancel"
                @click="emit('close', false)" />
        </template>

        <FloatLabel variant="on">
            <Textarea id="description" v-model="descriptionModel" rows="5" fluid autoResize />
            <label for="description">{{ $t('tasks.description') }}</label>
        </FloatLabel>

        <FloatLabel variant="on" class="task-edit-date-field">
            <DatePicker v-model="dueDateAsDate" inputId="due-date-picker" fluid />
            <label for="due-date-picker" class="task-edit-date-label">{{ $t('tasks.duedate') }}</label>
        </FloatLabel>

        <Message v-if="errorText.length" severity="error" icon="pi pi-times-circle" :life="3000">
            {{ errorText }}
        </Message>
        <template #footer>
            <Button v-if="isEdit" :label="t('tasks.editdialog.delete')" severity="danger" @click="onDelete"
                :loading="isDeleteLoading" />
            <Button :label="isEdit ? t('tasks.editdialog.save') : t('tasks.editdialog.create')" :disabled="!canSave"
                @click="onSave" :loading="isSaveLoading" />
        </template>
    </Dialog>
</template>

<style>
.p-dialog {
    width: 95%;
}

.p-inputtext.task-edit-title {
    border: none;
    box-shadow: none;
    background-color: transparent;
}

.p-inputtext.task-edit-title.completed {
    text-decoration: line-through
}

.p-dialog-header.task-edit-header {
    padding: 1rem;
}

.p-dialog-content.task-edit-content {
    padding-top: 0.25rem;
}

.p-floatlabel.task-edit-date-field {
    margin-top: 0.75rem;
}

#due-date-picker {
    background: transparent;
    border: none;
    text-align: center;
}

.p-floatlabel label.task-edit-date-label {
    left: 50%;
    transform: translateX(-50%);
}

.p-floatlabel-on:has(input:focus) label.task-edit-date-label {
    transform: translate(-50%, -50%);
}

.p-floatlabel-on:has(input.p-filled) label.task-edit-date-label {
    transform: translate(-50%, -50%);
}
</style>
