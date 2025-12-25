<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { tasksApi } from '@/api/TasksAPI'
import TaskCard from '@/components/TaskCard.vue'
import type { TaskItem } from '@/models/TaskItem'

const tasks = ref<TaskItem[]>([])

onMounted(async () => {
  tasks.value = await tasksApi.getTasks()
})
</script>

<template>
  <div class="tasks-view">
    <TaskCard
      v-for="task in tasks"
      :key="task.id"
      :task="task"
    />
  </div>
</template>

<style scoped>
.tasks-view {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 12px;
}
</style>
