import { nextTick, onMounted, onUnmounted, ref, watch, type Ref } from 'vue'

/** Measure the natural inner content, so short notes never get an expand control. */
export function useClampedContent(
  content: Ref<HTMLElement | null>,
  source: () => string,
  lines = 4,
) {
  const expanded = ref(false),
    foldable = ref(false)
  let observer: ResizeObserver | undefined
  function measure() {
    const element = content.value
    if (!element) return
    const lineHeight = parseFloat(getComputedStyle(element).lineHeight) || 22
    foldable.value = element.scrollHeight > lineHeight * lines + 2
    if (!foldable.value) expanded.value = false
  }
  onMounted(() => {
    observer = new ResizeObserver(measure)
    if (content.value) observer.observe(content.value)
    measure()
  })
  watch(source, async () => {
    await nextTick()
    measure()
  })
  onUnmounted(() => observer?.disconnect())
  function toggle(event?: MouseEvent) {
    if (event?.target instanceof Element && event.target.closest('a')) return
    if (foldable.value) expanded.value = !expanded.value
  }
  return { expanded, foldable, toggle }
}
