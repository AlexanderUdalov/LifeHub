<script setup lang="ts">
import { journalApi } from '@/api/JournalAPI';
import type { JournalItem } from '@/models/JournalItem';
import { onMounted, ref } from 'vue';

const items = ref<JournalItem[]>([])

onMounted(async () => {
  items.value = await journalApi.getItems()
})
</script>

<template>
    Journal placeholder
</template>

<style></style>