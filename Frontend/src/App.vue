<script setup lang="ts">
import { RouterView } from 'vue-router';

// temporary
import TaskCard from './components/TaskCard.vue';
import type { TaskDTO } from './api/TasksAPI';
const task: TaskDTO = {
  id: "string",
  title: "Test task name",
  description: "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
  dueDate: new Date(2026, 0, 26, 17, 30).toISOString(),
  completionDate: null,
  goalId: null
}
const task1: TaskDTO = {
  id: "string",
  title: "Lorem ipsum dolor sit amet, consectetur adipiscing elit",
  description: null,
  dueDate: new Date(2026, 0, 25, 17, 0).toISOString(),
  completionDate: null,
  goalId: null
}
const task2: TaskDTO = {
  id: "string",
  title: "Test task name",
  description: "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
  dueDate: null,
  completionDate: null,
  goalId: null
}
</script>

<template>
  <div class="app-shell">
      <!-- <RouterView /> -->
      <TaskCard :task="task"></TaskCard>
      <TaskCard :task="task1"></TaskCard>
      <TaskCard :task="task2"></TaskCard>
  </div>
</template>

<style>
.app-shell {
  background: var(--surface-100);
}

.phone-frame {
  width: 100%;
  max-width: 430px;
  background: var(--surface-0);
  box-shadow: 0 0 30px rgba(0, 0, 0, 0.08);
  border-radius: 16px;
  overflow: hidden;
}
</style>