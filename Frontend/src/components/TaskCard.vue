<script setup lang="ts">
import Card from 'primevue/card'
import Checkbox from 'primevue/checkbox'
import { computed, onMounted, ref, watch } from 'vue'
import type { TaskItem } from '@/models/TaskItem'
import type { ColoredTagEntity } from '@/models/ColoredTagEntity';
import SwipeableCard from './SwipeableCard.vue';

const props = defineProps<{ task: TaskItem }>()
defineEmits<{
  (e: 'edit', task: TaskItem): void
}>()

const localCompleted = ref<boolean>(!!props.task.completeDate)
const goal = ref<ColoredTagEntity | null>(null)

watch(localCompleted, value => {
  console.log('Task completed changed:', value)
})

const isOverdue = computed(() => {
  if (!props.task.dueDate || localCompleted.value) return false
  return new Date(props.task.dueDate) < new Date()
})

const deadlineText = computed(() => {
  if (!props.task.dueDate) return null
  return new Date(props.task.dueDate).toLocaleDateString()
})

const descriptionPreview = computed(() => {
  if (!props.task.description) return ''

  const div = document.createElement('div')
  div.innerHTML = props.task.description

  return div.textContent?.replace(/\s+/g, ' ').trim() ?? ''
})

onMounted(async () => {
  goal.value = { id: 1, title: 'GoalName', color: '#ff0000' };
})

// временно
const isRecurring = true

const remove = () => { }
const archive = () => { }
</script>


<template>
  <SwipeableCard :right-actions="[
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
  ]" @open="$emit('edit', task)">
    <Card class="task-card" :style="{ '--goal-color': goal?.color ?? 'transparent' }">
      <template #content>
        <div class="task-content">
          <Checkbox v-model="localCompleted" binary @click.stop />
          <div class="task-body">
            <div class="task-title" :class="{ completed: localCompleted }">
              {{ task.title }}
            </div>

            <div v-if="descriptionPreview" class="task-description">
              {{ descriptionPreview }}
            </div>

            <div class="task-meta">
              <span v-if="deadlineText" class="deadline" :class="{
                overdue: isOverdue,
                completed: localCompleted
              }">
                {{ deadlineText }}
              </span>

              <i v-if="isRecurring" class="pi pi-refresh recurring" />

              <span v-if="goal" class="goal-title">
                {{ goal.title }}
              </span>
            </div>
          </div>
        </div>
      </template>
    </Card>
  </SwipeableCard>
</template>

<style scoped>
.task-card {
  border-left: 4px solid var(--goal-color);
  box-shadow: 0px 0px 5px 1px rgba(0, 0, 0, 0.1);
}

.task-content {
  display: flex;
  gap: 12px;
  align-items: center;
}

:deep(.p-card-body) {
  padding: 0.6rem;
}

.task-body {
  flex: 1;
  min-width: 0;
}

/* TITLE */
.task-title {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 10px;
}

.task-title.completed {
  text-decoration: line-through;
  opacity: 0.6;
}

/* DESCRIPTION */
.task-description {
  font-size: 13px;
  color: var(--p-text-muted-color);
  margin-bottom: 4px;

  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* META */
.task-meta {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: var(--p-text-muted-color);
}

.deadline {
  color: var(--p-text-muted-color);
}

.deadline.overdue {
  color: #d32f2f;
}

.deadline.completed {
  color: var(--p-text-muted-color);
}

.recurring {
  font-size: 12px;
}

.goal-title {
  margin-left: auto;
  color: var(--p-text-muted-color);
}
</style>
