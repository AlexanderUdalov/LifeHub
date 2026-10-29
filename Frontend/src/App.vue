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
  min-height: 100dvh;
  background: var(--p-content-background);
}
</style>