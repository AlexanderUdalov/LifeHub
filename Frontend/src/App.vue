<script setup lang="ts">
import { RouterView } from 'vue-router';
</script>

<template>
  <div class="app-shell">
    <div class="phone-frame">
      <RouterView />
    </div>
  </div>
</template>

<style>
.app-shell {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  background: var(--surface-100);
}

.phone-frame {
  width: 100%;
  max-width: 430px;
  min-height: 100vh;
  background: var(--surface-0);
  display: flex;
  flex-direction: column;
  box-shadow: 0 0 30px rgba(0, 0, 0, 0.08);
  border-radius: 16px;
  overflow: hidden;
}
</style>