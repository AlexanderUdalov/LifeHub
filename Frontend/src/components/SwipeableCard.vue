<script setup lang="ts">
import { ref, computed } from 'vue'
import { useDrag } from '@vueuse/gesture'

interface SwipeAction {
    icon: string
    bg: string
    onClick: () => void
}

const props = defineProps<{
    leftActions?: SwipeAction[]
    rightActions?: SwipeAction[]
}>()

const emit = defineEmits<{
    (e: 'open'): void
}>()

const dragEl = ref<HTMLElement | null>(null)
const x = ref(0)

const ACTION_WIDTH = 72
const MAX_LEFT = computed(() => (props.leftActions?.length ?? 0) * ACTION_WIDTH)
const MAX_RIGHT = computed(() => (props.rightActions?.length ?? 0) * ACTION_WIDTH)

useDrag(
    ({ movement: [mx], last, tap }) => {
        if (tap) return

        x.value = Math.max(
            -MAX_RIGHT.value,
            Math.min(MAX_LEFT.value, mx)
        )

        if (last) {
            // snap
            if (x.value > ACTION_WIDTH / 2) x.value = MAX_LEFT.value
            else if (x.value < -ACTION_WIDTH / 2) x.value = -MAX_RIGHT.value
            else x.value = 0
        }
    },
    {
        domTarget: dragEl,
        axis: 'x',
        filterTaps: true,
        threshold: 10
    }
)

const onClick = () => {
    if (Math.abs(x.value) > 5) return
    emit('open')
}

const onActionClick = (action: SwipeAction) => {
    action.onClick()
    x.value = 0
}
</script>

<template>
    <div class="swipe-root">
        <!-- LEFT ACTIONS -->
        <div class="swipe-actions left">
            <button v-for="(action, i) in leftActions" :key="i" class="swipe-action" :style="{ background: action.bg }"
                @click.stop="onActionClick(action)">
                <i :class="action.icon" />
            </button>
        </div>

        <!-- RIGHT ACTIONS -->
        <div class="swipe-actions right">
            <button v-for="(action, i) in rightActions" :key="i" class="swipe-action" :style="{ background: action.bg }"
                @click.stop="onActionClick(action)">
                <i :class="action.icon" />
            </button>
        </div>

        <!-- CONTENT -->
        <div ref="dragEl" class="swipe-card" :style="{ transform: `translateX(${x}px)` }" @click="onClick">
            <slot />
        </div>
    </div>
</template>

<style>
.swipe-root {
    position: relative;
    overflow: hidden;
}

.swipe-card {
    will-change: transform;
    transition: transform 0.25s ease;
    padding: 5px;
}

.swipe-actions {
    position: absolute;
    top: 0;
    bottom: 0;
    display: flex;
}

.swipe-actions.left {
    left: 0;
    flex-direction: row;
}

.swipe-actions.right {
    right: 0;
    flex-direction: row-reverse;
}

.swipe-action {
    width: 72px;
    border: none;
    color: white;
    font-size: 18px;
    display: flex;
    align-items: center;
    justify-content: center;
}
</style>