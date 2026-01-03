<script setup lang="ts">
import { addictionsApi } from '@/api/AddictionAPI';
import { journalApi } from '@/api/JournalAPI';
import JournalCard from '@/components/JournalCard.vue';
import Select from 'primevue/select';
import DatePicker from 'primevue/datepicker';
import Button from 'primevue/button';
import type { ColoredTagEntity } from '@/models/ColoredTagEntity';
import type { JournalItem } from '@/models/JournalItem';
import { computed, onMounted, ref } from 'vue';

const items = ref<JournalItem[]>([])
const goals = ref<ColoredTagEntity[]>([])
const addictions = ref<ColoredTagEntity[]>([])

const selectedGoal = ref<ColoredTagEntity | null>(null)
const selectedAddiction = ref<ColoredTagEntity | null>(null)
const selectedDateRange = ref<[Date, Date] | null>(null)

onMounted(async () => {
  items.value = await journalApi.getItems()
  goals.value = [{ id: 1, title: "TestGoal1", color: "#FA1110" }, { id: 2, title: "TestGoal2", color: "00FA44" }]
  addictions.value = await addictionsApi.getAddictionsAsTags()
})

const filteredItems = computed(() => {
  return items.value.filter(item => {
    if (selectedGoal.value && item.goalId !== selectedGoal.value.id) {
      return false
    }

    if (selectedAddiction.value && item.addictionId !== selectedAddiction.value.id) {
      return false
    }

    if (selectedDateRange.value) {
      const [from, to] = selectedDateRange.value
      const itemDate = new Date(item.date)

      if (itemDate < from || itemDate > to) {
        return false
      }
    }

    return true
  })
})

function resetFilters() {
  selectedGoal.value = null
  selectedAddiction.value = null
  selectedDateRange.value = null
}
</script>

<template>
  <div class="filter-bar">
    <Select v-model="selectedGoal" :options="goals" optionLabel="title" placeholder="Goal" showClear
      class="filter-item" />

    <Select v-model="selectedAddiction" :options="addictions" optionLabel="title" placeholder="Addiction" showClear
      class="filter-item" />

    <DatePicker v-model="selectedDateRange" selectionMode="range" placeholder="Dates" dateFormat="dd.mm.yy"
      class="filter-item" />

    <Button icon="pi pi-times" @click="resetFilters" />
  </div>

  <JournalCard v-for="item in filteredItems" :key="item.id" :item="item" />
</template>

<style scoped>
.filter-bar {
  display: flex;
  gap: 0.75rem;
  align-items: center;
  margin-bottom: 1rem;
  flex-wrap: wrap;
}

.filter-item {
  min-width: 14rem;
}

.journal-list {
  display: flex;
  flex-direction: column;
}
</style>
