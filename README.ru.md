# LifeHub

[English version](README.md)

LifeHub - это персональный центр управления ростом для людей, которым нужен не просто еще один список задач.

Проект объединяет задачи, привычки, цели, дневник, трекинг зависимостей, сферы жизни и AI-рефлексию в одном связанном рабочем пространстве. Вместо того чтобы держать планы, рутины, заметки и срывы в разных приложениях, LifeHub помогает видеть общую картину: что вы хотите изменить, что делаете сегодня, что мешает прогрессу и как меняется ваша история со временем.

## Зачем нужен LifeHub

Большинство инструментов для саморазвития решают одну узкую задачу. LifeHub построен вокруг идеи, что личные изменения связаны между собой:

- Цели становятся конкретными через задачи, привычки и планы восстановления.
- Привычки формируют регулярность и не живут отдельно от остальной жизни.
- Дневник сохраняет контекст, эмоции, выводы и важные поворотные моменты.
- Трекинг зависимостей учитывает срывы, триггеры и восстановление как полноценную часть прогресса.
- AI-помощник превращает заметки и поведение в вопросы для рефлексии, сводки и практические следующие шаги.

LifeHub создан как приватное и структурированное место, где можно планировать лучшие дни, понимать свои паттерны и двигаться дальше, не теряя человеческий контекст за цифрами.

## Основные возможности

- Единые продуктовые разделы: задачи, привычки, зависимости, сферы жизни, цели, дневник, профиль и авторизация.
- Планирование вокруг целей: связь между тем, чего вы хотите, и конкретными действиями, которые к этому ведут.
- Трекинг привычек для регулярного поведения, дисциплины и долгосрочной устойчивости.
- Трекинг зависимостей с ресетами, позывами, триггерами, заметками и отдельными страницами деталей.
- Дневник для ежедневной рефлексии, контекста и истории прогресса.
- AI-рефлексия через LLM API, если настроен API-ключ.
- Архитектура с приоритетом API и генерацией TypeScript-типов для фронтенда.
- Web-приложение с опциональной поддержкой desktop-сборки через Tauri.

## Статус проекта

LifeHub - персональный проект с завершенным набором основных функций для повседневного использования. Дальнейшая работа предполагается в основном вокруг поддержки, исправления ошибок и небольших улучшений качества использования.

## Технический стек

| Область | Технологии |
| --- | --- |
| Frontend | Vue 3, Vite 7, TypeScript, Pinia, Vue Router, PrimeVue, vue-i18n, Zod |
| Backend | ASP.NET Core Web API, .NET 10, JWT Bearer-аутентификация |
| Database | SQLite через Entity Framework Core |
| AI | Microsoft.Extensions.AI с OpenAI-compatible chat client |
| API Contracts | ASP.NET OpenAPI + `openapi-typescript` для генерации frontend-схемы |
| Testing | Vitest для frontend, xUnit для backend |
| Desktop | Опциональный workflow на Tauri 2 |
| Deployment | GitHub Actions, загрузка артефактов на VPS, rsync, перезапуск backend через systemd |

## Структура репозитория

```text
LifeHub/
├── Backend/          # ASP.NET Core API, EF Core models, controllers, services
├── Backend.Tests/    # xUnit-тесты backend
├── Frontend/         # Vue 3 + Vite application
├── Docs/             # Product documentation
└── .github/          # CI/CD workflow для deployment
```

## Быстрый старт

### Требования

- .NET SDK 10.x
- Node.js 20.19+ или 22.12+
- npm
- EF Core CLI tools для миграций БД
- Опционально: Python или `sqlite3` для очистки зависшей блокировки SQLite-миграций
- Опционально: требования Tauri для desktop-сборок

### Установка зависимостей

```bash
npm install
npm install --prefix Frontend
dotnet restore
```

### Локальные секреты

Backend требует JWT signing key. AI необязателен, но AI-функции требуют API-ключ.

```bash
dotnet user-secrets set "Jwt:Key" "replace-with-a-long-random-secret" --project Backend/LifeHub-Backend.csproj
dotnet user-secrets set "Ai:ApiKey" "replace-with-your-openai-key" --project Backend/LifeHub-Backend.csproj
```

Подробнее о локальных и production-секретах см. в [`Backend/SECRETS.md`](Backend/SECRETS.md).

### Применение миграций

```bash
dotnet ef database update --project Backend/LifeHub-Backend.csproj
```

Сейчас приложение использует SQLite. Во время запуска файл базы хранится в выходной директории backend в `database/lifehub.db`.

### Запуск приложения

Из корня репозитория:

```bash
npm run dev
```

Команда запускает оба сервиса:

- Backend API: `http://localhost:5091`
- Frontend Vite dev server: обычно `http://localhost:5173`

Vite dev server проксирует `/api` запросы на backend.

Отдельный запуск:

```bash
npm run dev:backend
npm run dev:frontend
```

## Полезные команды

| Команда | Где запускать | Описание |
| --- | --- | --- |
| `npm run dev` | root | Запустить backend и frontend вместе |
| `npm run dev:backend` | root | Запустить только ASP.NET Core API |
| `npm run dev:frontend` | root | Запустить только Vite frontend |
| `npm run build` | `Frontend/` | Type-check и production-сборка frontend |
| `npm run preview` | `Frontend/` | Preview production-сборки frontend |
| `npm run type-check` | `Frontend/` | TypeScript-проверки Vue |
| `npm run test` | `Frontend/` | Frontend-тесты через Vitest |
| `npm run format` | `Frontend/` | Форматирование исходников frontend |
| `npm run api:gen` | `Frontend/` | Перегенерировать TypeScript API-типы из backend OpenAPI |
| `dotnet test` | root | Запустить backend-тесты |
| `npm run dev:tauri` | `Frontend/` | Запустить frontend в режиме для Tauri |
| `npm run build:tauri` | `Frontend/` | Собрать frontend bundle для Tauri |

## Workflow API-схемы

Frontend использует сгенерированные API-типы из `Frontend/src/api/schema.ts`.

Когда меняются backend DTO, controllers или API contracts:

1. Запустите backend в Development mode.
2. Выполните `npm run api:gen` из `Frontend/`.
3. Закоммитьте сгенерированную схему вместе с API-изменениями.
4. Остановите backend после генерации.

OpenAPI доступен в Development по адресу `http://localhost:5091/openapi/v1.json`.

## Развертывание

В репозитории есть GitHub Actions workflow, который запускается при push в `main`.

Workflow:

- Публикует backend через `dotnet publish`.
- Устанавливает зависимости и собирает frontend через npm.
- Загружает артефакты на VPS.
- Синхронизирует backend и frontend в `/var/www/lifehub`.
- Записывает production-секреты в backend `.env`.
- Перезапускает systemd-сервис `lifehub-backend`.

Нужные GitHub Secrets:

- `VPS_HOST`
- `VPS_USER`
- `VPS_KEY`
- `JWT_KEY`
- `AI_API_KEY`

## Нюансы SQLite-миграций

EF Core защищает SQLite-миграции таблицей `__EFMigrationsLock`. Если миграция была прервана или другой процесс держит базу открытой, устаревшая блокировка может мешать следующим миграциям.

Сначала остановите backend и любые процессы, которые используют `lifehub.db`, затем очистите блокировку:

```bash
sqlite3 Backend/bin/Debug/database/lifehub.db "DROP TABLE IF EXISTS \"__EFMigrationsLock\";"
```

Если `sqlite3` не установлен, используйте вспомогательный скрипт:

```bash
python Backend/scripts/drop_migrations_lock.py
```

После этого повторите команду миграции.

## Поддержка

Проект остается сфокусированным на текущем продуктовом объеме. Поддержка может включать исправление багов, обновление зависимостей, улучшение документации и небольшие доработки существующего опыта.

## Документация

- [`Frontend/docs/design-system.md`](Frontend/docs/design-system.md) - заметки по frontend design system.
- [`Backend/SECRETS.md`](Backend/SECRETS.md) - настройка локальных и production-секретов.

## Участие в разработке

LifeHub является персональным проектом. Bug reports, исправления документации и сфокусированные улучшения приветствуются.

Перед pull request желательно запустить релевантные проверки:

```bash
npm run build --prefix Frontend
npm run test --prefix Frontend
dotnet test
```

## Лицензия

LifeHub распространяется по лицензии [MIT](LICENSE).
