<script setup lang="ts">
import Card from 'primevue/card'
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import { computed, onMounted, ref } from 'vue';
import type { JournalItem } from '@/models/JournalItem';
import type { ColoredTagEntity } from '@/models/ColoredTagEntity';

const props = defineProps<{ item: JournalItem }>()
const emit = defineEmits<{
    (e: 'update', item: JournalItem): void
    (e: 'delete', id: number): void
}>()

const isEditOpen = ref(false)
const isDeleteOpen = ref(false)
const editedText = ref(props.item.text)

const goal = ref<ColoredTagEntity | null>(null)
const addiction = ref<ColoredTagEntity | null>(null)

const formattedDate = computed(() =>
    new Date(props.item.date).toLocaleDateString('ru-RU', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
    })
)

onMounted(async () => {
    if (props.item.goalId) {
        goal.value = { id: 1, title: 'GoalName', color: '#ff0000' };
    }
    if (props.item.addictionId) {
        addiction.value = { id: 1, title: 'AddictionName', color: '#00ff00' };
    }
})

function deleteItem() {

}

function saveEdit() {

}

</script>

<template>
    <Card class="journal-card">
        <template #title>
            <div class="header">
                <span class="date">📅 {{ formattedDate }} </span>
                <div class="actions">
                    <Button icon="pi pi-pencil" variant="text" rounded size="small" @click="isEditOpen = true" />
                    <Button icon="pi pi-trash" variant="text" rounded size="small" severity="danger"
                        @click="isDeleteOpen = true" />
                </div>
            </div>

        </template>

        <template #content>
            <p class="text">
                {{ item.text }}
            </p>

            <div class="tags">
                <Tag v-if="goal" :value="goal.title" :style="{ backgroundColor: goal.color }" icon="pi pi-flag" />

                <Tag v-if="addiction" :value="addiction.title" :style="{ backgroundColor: addiction.color }"
                    icon="pi pi-bolt" />
            </div>
        </template>
    </Card>

    <!-- Edit dialog -->
    <Dialog v-model:visible="isEditOpen" header="Edit item" modal style="width: 30rem">
        <Textarea v-model="editedText" autoResize rows="5" class="w-full" />

        <template #footer>
            <Button label="Cancel" text @click="isEditOpen = false" />
            <Button label="Save" @click="saveEdit" />
        </template>
    </Dialog>

    <!-- Delete dialog -->
    <Dialog v-model:visible="isDeleteOpen" header="Delete item" modal>
        <p>Are you sure?</p>
        <template #footer>
            <Button label="Cancel" text @click="isDeleteOpen = false" />
            <Button label="Delete"  severity="danger" @click="deleteItem" />
        </template>
    </Dialog>

</template>

<style scoped>
.journal-card {
    margin-bottom: 1rem;
}

.header {
    display: flex;
    justify-content: space-between;
}

.actions {
    display: flex;
    justify-content: flex-end;
}

.date {
    font-size: 0.9rem;
    color: var(--text-color-secondary);
}

.text {
    white-space: pre-wrap;
    margin-bottom: 1rem;
}

.tags {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
}
</style>