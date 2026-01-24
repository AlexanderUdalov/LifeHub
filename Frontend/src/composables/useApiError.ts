import { useI18n } from 'vue-i18n'
import { ApiError } from '@/api/Errors'

const errorCodeToI18nKey: Record<string, string> = {
    INVALID_CREDENTIALS: 'errors.invalidCredentials',
    EMAIL_EXISTS: 'errors.emailAlreadyExists'
}

export function useApiError() {
    const { t } = useI18n()

    function resolveMessage(error: unknown): string {
        if (error instanceof ApiError) {
            return t(
                errorCodeToI18nKey[error.code] ?? 'errors.unknown'
            )
        }

        return t('errors.unknown')
    }

    return {
        resolveMessage
    }
}
