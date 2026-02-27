<script setup lang="ts">
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import Message from 'primevue/message'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { GoalDTO } from '@/api/GoalsAPI'
import { useGoalsStore } from '@/stores/goals'
import { useLifeAreasStore } from '@/stores/lifeAreas'
import { useApiError } from '@/composables/useApiError'

const props = defineProps<{
  goal: GoalDTO | null
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const { t } = useI18n()
const goalsStore = useGoalsStore()
const lifeAreasStore = useLifeAreasStore()
const apiError = useApiError()

const localTitle = ref(props.goal?.title ?? '')
const localDescription = ref(props.goal?.description ?? '')
const localDueDate = ref<Date>(props.goal?.dueDate ? new Date(props.goal.dueDate) : new Date())
const localLifeAreaId = ref<string | null>(props.goal?.lifeAreaId ?? null)

const isEdit = computed(() => !!props.goal)
const canSave = computed(() => localTitle.value.trim().length > 0 && !!localDueDate.value)

const isSaveLoading = ref(false)
const isDeleteLoading = ref(false)
const errorText = ref('')

async function onSave() {
  if (!canSave.value) return
  errorText.value = ''
  isSaveLoading.value = true
  try {
    if (isEdit.value) {
      await goalsStore.updateGoal(props.goal!.id, {
        title: localTitle.value.trim(),
        description: localDescription.value.trim() || null,
        dueDate: localDueDate.value.toISOString(),
        lifeAreaId: localLifeAreaId.value
      })
    } else {
      await goalsStore.createGoal({
        title: localTitle.value.trim(),
        description: localDescription.value.trim() || null,
        dueDate: localDueDate.value.toISOString(),
        lifeAreaId: localLifeAreaId.value
      })
    }
    emit('close')
  } catch (e) {
    errorText.value = apiError.resolveMessage(e)
  } finally {
    isSaveLoading.value = false
  }
}

async function onDelete() {
  if (!props.goal) return
  errorText.value = ''
  isDeleteLoading.value = true
  try {
    await goalsStore.deleteGoal(props.goal.id)
    emit('close')
  } catch (e) {
    errorText.value = apiError.resolveMessage(e)
  } finally {
    isDeleteLoading.value = false
  }
}

</script>

<template>
  <Dialog modal :visible="true" :closable="false" :draggable="false" style="width: 90vw; max-width: 420px" @hide="emit('close')">
    <template #header>
      <div class="goal-edit-header">
        <InputText id="goal-title" v-model="localTitle" class="goal-edit-title" :placeholder="t('goals.newGoal')" size="large" />
        <Button icon="pi pi-times" severity="secondary" rounded variant="outlined" aria-label="Cancel" size="small" @click="emit('close')" />
      </div>
    </template>

    <div class="form-field">
      <label class="field-label">{{ t('tasks.description') }}</label>
      <Textarea v-model="localDescription" rows="3" autoResize />
    </div>

    <div class="form-field">
      <label class="field-label">{{ t('goals.dueDate') }}</label>
      <DatePicker v-model="localDueDate" date-format="dd.mm.yy" show-icon fluid />
    </div>

    <div class="form-field">
      <label class="field-label">{{ t('lifeareas.field') }}</label>
      <Select
        v-model="localLifeAreaId"
        :options="lifeAreasStore.lifeAreas"
        option-label="name"
        option-value="id"
        show-clear
        :placeholder="t('lifeareas.selectPlaceholder')"
      />
    </div>

    <Message v-if="errorText.length" severity="error" icon="pi pi-times-circle" :life="3000">
      {{ errorText }}
    </Message>

    <template #footer>
      <Button v-if="isEdit" :label="t('goals.delete')" severity="danger" :loading="isDeleteLoading" @click="onDelete" />
      <Button :label="isEdit ? t('goals.save') : t('goals.create')" :disabled="!canSave" :loading="isSaveLoading" @click="onSave" />
    </template>
  </Dialog>
</template>

<style scoped>
.goal-edit-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
}

.goal-edit-title {
  flex: 1;
  min-width: 0;
  border: none;
  box-shadow: none;
  background-color: transparent;
}

.form-field {
  margin-bottom: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  width: 100%;
}

.field-label {
  font-size: 0.875rem;
  color: var(--p-text-muted-color);
}
</style>