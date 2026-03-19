import { useI18n } from 'vue-i18n'
import { startOfDay } from '@/utils/dateOnly'

function diffInDays(from: Date, to: Date) {
    const msPerDay = 24 * 60 * 60 * 1000
    return Math.round(
        (startOfDay(to).getTime() - startOfDay(from).getTime()) / msPerDay
    )
}

export function useDeadlineFormatter() {
    const { t } = useI18n()
    const { locale } = useI18n()

    function getDaysDiffString(target: Date): string {
        const now = new Date()
        const due = new Date(target)
        const daysDiff = diffInDays(now, due)

        if (daysDiff === 0) return t("tasks.dates.today")
        if (daysDiff === 1) return t("tasks.dates.tomorrow")
        if (daysDiff === -1) return t("tasks.dates.yesterday")

        if (daysDiff > 1 && daysDiff <= 4) {
            return t('tasks.dates.inDays', { count: daysDiff }, daysDiff)
        }

        if (daysDiff < -1 && daysDiff >= -4) {
            return t('tasks.dates.daysAgo', { count: Math.abs(daysDiff) }, Math.abs(daysDiff))
        }

        return due.toLocaleDateString(locale.value)
    }

    function formatDeadline(date: Date): string {
        const dayText = getDaysDiffString(date)
        // For due dates we only show the calendar day (no time-of-day).
        // Time may exist in the DateTimeOffset, but it causes TZ shifts and confusing UI.
        return dayText
    }

    return {
        formatDeadline,
    }
}
