<script setup lang="ts">
import Card from 'primevue/card'
import Checkbox from 'primevue/checkbox'
import { ref, watch } from 'vue'
import type { TaskItem } from '@/models/TaskItem'

const props = defineProps<{ task: TaskItem }>()
const localCompleted = ref<boolean>(!!props.task.completeDate)

watch(localCompleted, value => {
  console.log('Task completed changed:', value)
})
</script>


<template>
  <Card class="task-card">
    <template #content>
      <div class="task-content">
        <Checkbox v-model="localCompleted" binary />

        <div class="task-text">
          <div class="task-title" :class="{ completed: localCompleted }">
            {{ task.title }}
          </div>

          <div v-if="task.description" class="task-description">
            {{ task.description }}
          </div>
        </div>
      </div>
    </template>
  </Card>
</template>

<style scoped>
.task-card {
  margin-bottom: 12px;
}

.task-content {
  display: flex;
  gap: 12px;
  align-items: center;
}

.task-text {
  flex: 1;
}

.task-title {
  font-size: 16px;
  font-weight: 500;
}

.task-title.completed {
  text-decoration: line-through;
  opacity: 0.6;
}

.task-description {
  font-size: 13px;
  color: #666;
}
</style>
