<script setup lang="ts">
import { RouterView } from 'vue-router';
</script>

<template>
  <div class="app-shell">
    <RouterView />
  </div>
</template>

<style>
.app-shell {
  background: var(--surface-100);
}
</style>