import { useI18n } from 'vue-i18n'

function startOfDay(date: Date) {
    const d = new Date(date)
    d.setHours(0, 0, 0, 0)
    return d
}

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

        if (daysDiff > 1 && daysDiff <= 7) {
            return t('tasks.dates.inDays', { count: daysDiff })
        }

        if (daysDiff < -1 && daysDiff >= -7) {
            return t('tasks.dates.daysAgo', { count: Math.abs(daysDiff) })
        }

        if (daysDiff > 0 && daysDiff <= 14) {
            return t('tasks.dates.nextWeekday', {
                weekday: due.toLocaleDateString(locale.value, { weekday: 'long' })
            })
        }

        return due.toLocaleDateString(locale.value)
    }

    function getTimeString(date: Date): string | null {
        const hours = date.getHours()
        const minutes = date.getMinutes()

        if (hours === 0 && minutes === 0) return null

        return date.toLocaleTimeString(locale.value, {
            hour: '2-digit',
            minute: '2-digit',
        })
    }

    function formatDeadline(date: Date): string {
        const dayText = getDaysDiffString(date)
        const timeText = getTimeString(date)
        return timeText ? `${dayText}, ${timeText}` : dayText
    }

    return {
        formatDeadline,
    }
}
