<script setup lang="ts">
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import FloatLabel from 'primevue/floatlabel'
import Textarea from 'primevue/textarea'
import Checkbox from 'primevue/checkbox'
import Message from 'primevue/message'
import Popover from 'primevue/popover'
import Select from 'primevue/select'
import { computed, onMounted, ref } from 'vue'
import { type CreateTaskRequest, type TaskDTO, type UpdateTaskRequest } from '@/api/TasksAPI'
import { useI18n } from 'vue-i18n'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useGoalsStore } from '@/stores/goals'
import DateAndRecurrencePicker from '@/components/DateAndRecurrencePicker.vue'
import { useRecurrenceFormatter } from '@/composables/useRecurrenceFormatter'

const { t } = useI18n()

const props = defineProps<{
    task: TaskDTO | null
}>()

import { useApiError } from "@/composables/useApiError";
const apiError = useApiError();

import { useTasksStore } from '@/stores/tasks'
const tasksStore = useTasksStore()
const lifeAreasStore = useLifeAreasStore()
const goalsStore = useGoalsStore()

const emit = defineEmits<{
    (e: 'close'): void
}>()

const localTask = ref<TaskDTO>({
    id: props.task?.id ?? '',
    title: props.task?.title ?? '',
    description: props.task?.description ?? null,
    dueDate: props.task?.dueDate ? props.task.dueDate : null,
    completionDate: props.task?.completionDate ? props.task.completionDate : null,
    recurrenceRule: props.task?.recurrenceRule ?? null,
    goalId: props.task?.goalId ? props.task.goalId : null,
    lifeAreaId: props.task?.lifeAreaId ? props.task.lifeAreaId : null,
    sortOrder: props.task?.sortOrder ?? null
})

const dueDateAsDate = computed<Date | null>({
    get() {
        return localTask.value.dueDate ? new Date(localTask.value.dueDate) : null
    },
    set(value) {
        localTask.value.dueDate = value?.toISOString() ?? null
    }
})

const isEdit = computed(() => !!props.task)
const canSave = computed(() => localTask.value.title.trim().length > 0)

const completedModel = computed({
    get() {
        return localTask.value.completionDate != null
    },
    set(value) {
        localTask.value.completionDate = value ? new Date().toISOString() : null
    }
})

const descriptionModel = computed({
    get() {
        return localTask.value.description ?? ''
    },
    set(value) {
        localTask.value.description = value || null
    }
})

const { formatRecurrence } = useRecurrenceFormatter()
const dateAndRepeatSummary = computed(() => {
    const d = dueDateAsDate.value
    const dateStr = d ? d.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' }) : t('tasks.recurrence.noDate')
    const recurStr = formatRecurrence(localTask.value.recurrenceRule, localTask.value.dueDate)
    return recurStr ? `${dateStr} • ${recurStr}` : dateStr
})

const dateRecurrencePopover = ref<InstanceType<typeof Popover> | null>(null)

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
                recurrenceRule: localTask.value.recurrenceRule,
                goalId: localTask.value.goalId,
                lifeAreaId: localTask.value.lifeAreaId,
                sortOrder: localTask.value.dueDate ? null : (localTask.value.sortOrder ?? null)
            }

            await tasksStore.editTask(localTask.value.id, request)
        } else {
            const request: CreateTaskRequest = {
                title: localTask.value.title,
                description: localTask.value.description,
                dueDate: localTask.value.dueDate,
                recurrenceRule: localTask.value.recurrenceRule,
                goalId: localTask.value.goalId,
                lifeAreaId: localTask.value.lifeAreaId
            }
            await tasksStore.createNewTask(request)
        }
        emit('close')
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
        await tasksStore.removeTask(localTask.value.id)
        emit('close')
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
    <Dialog modal :visible="true" :closable="false" :draggable="false" @hide="emit('close')" :pt="{
        header: { class: 'task-edit-header' },
        content: { class: 'task-edit-content' }
    }">
        <template #header>
            <div class="task-edit-header-left">
                <Checkbox v-if="isEdit" binary v-model="completedModel" />
                <InputText id="title" class="task-edit-title" :class="{ completed: completedModel }"
                    v-model="localTask.title" :placeholder="t('tasks.editdialog.newTask')" size="large" />
            </div>
            <div class="task-edit-close-wrap">
                <Button icon="pi pi-times" severity="secondary" rounded variant="outlined" aria-label="Cancel"
                    size="small" @click="emit('close')" />
            </div>
        </template>

        <FloatLabel variant="on">
            <Textarea id="description" v-model="descriptionModel" rows="5" fluid autoResize />
            <label for="description">{{ $t('tasks.description') }}</label>
        </FloatLabel>

        <div class="task-edit-section task-edit-date-and-repeat">
            <label class="task-edit-field-label">{{ $t('tasks.recurrence.dateAndRepeat') }}</label>
            <Button type="button" :label="dateAndRepeatSummary" icon="pi pi-calendar" class="date-repeat-trigger"
                @click="(e: Event) => dateRecurrencePopover?.toggle(e)" />
            <Popover ref="dateRecurrencePopover" append-to="body">
                <DateAndRecurrencePicker :date="dueDateAsDate" :recurrence-rule="localTask.recurrenceRule"
                    @update:date="(v) => { localTask.dueDate = v?.toISOString() ?? null }"
                    @update:recurrence-rule="(v) => { localTask.recurrenceRule = v }"
                    @close="dateRecurrencePopover?.hide()" />
            </Popover>
        </div>

        <div class="task-edit-section">
            <label class="task-edit-field-label">{{ t('lifeareas.field') }}</label>
            <Select v-model="localTask.lifeAreaId" class="lifearea-select" :options="lifeAreasStore.lifeAreas"
                option-label="name" option-value="id" show-clear :placeholder="t('lifeareas.selectPlaceholder')" />
        </div>

        <div class="task-edit-section">
            <label class="task-edit-field-label">{{ t('goals.field') }}</label>
            <Select v-model="localTask.goalId" class="goal-select" :options="goalsStore.goalsSorted"
                option-label="title" option-value="id" show-clear :placeholder="t('goals.selectPlaceholder')" />
        </div>

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
    flex: 1;
    min-width: 0;
    border: none;
    box-shadow: none;
    background-color: transparent;
}

.p-inputtext.task-edit-title.completed {
    text-decoration: line-through
}

.p-dialog-header.task-edit-header {
    position: relative;
    padding: 1rem;
    padding-right: 3rem;
}

.task-edit-header-left {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    min-width: 0;
    flex: 1;
}

.task-edit-close-wrap {
    position: absolute;
    top: 1rem;
    right: 1rem;
}

.p-dialog-content.task-edit-content {
    padding-top: 0.25rem;
}

.task-edit-section {
    margin-top: 1rem;
}

.task-edit-section .task-edit-field-label {
    display: block;
    font-size: 0.875rem;
    color: var(--p-text-muted-color);
    margin-bottom: 0.5rem;
}

.task-edit-date-and-repeat .date-repeat-trigger {
    width: 100%;
    justify-content: center;
}

.task-edit-section .lifearea-select {
    width: 100%;
}

.task-edit-section .goal-select {
    width: 100%;
}
</style>
