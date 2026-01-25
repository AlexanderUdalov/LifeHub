import axios from "axios";
import { useI18n } from "vue-i18n";

export function useApiError() {
    const { t } = useI18n()

    type ErrorMapping = {
        [key: string]: string;
    };

    const apiErrorMap: ErrorMapping = {
        "400": t("errors.invalidCredentials"),
        "500": t("errors.serverError"),
        // "400:EMAIL_EXISTS": "errors.emailAlreadyExists",
        // "401:INVALID_TOKEN": "errors.invalidToken"
    };

    const resolveMessage = (e: unknown): string => {
        if (!axios.isAxiosError(e)) return t("errors.unknown");

        const { status, data } = e.response ?? {};
        const code = data?.code;

        if (status && code && apiErrorMap[`${status}:${code}`]) {
            return apiErrorMap[`${status}:${code}`]!;
        }

        if (status && apiErrorMap[`${status}`]) {
            return apiErrorMap[`${status}`]!;
        }


        return t("errors.unknown");
    }

    return { resolveMessage }
}
