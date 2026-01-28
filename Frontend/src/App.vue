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

.phone-frame {
  width: 100%;
  max-width: 430px;
  background: var(--surface-0);
  box-shadow: 0 0 30px rgba(0, 0, 0, 0.08);
  border-radius: 16px;
  overflow: hidden;
}
</style>