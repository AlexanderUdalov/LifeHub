# LifeHub

LifeHub is an all-in-one personal growth platform that unifies tasks, habits, goals, journaling, addiction tracking, and AI-powered reflection.  
A single place to manage your life, build discipline, and understand yourself through data and structure.

---

## 📌 Vision

Modern self-development tools are fragmented: one app for tasks, another for habits, another for mood or addiction tracking, and none of them understand the full picture.

**LifeHub aims to become a unified operating center for personal growth**, where all aspects of self-improvement work together:
- Tasks support goals  
- Habits build consistency  
- Reflection reveals patterns  
- AI helps analyze your state and progress  
- All data is connected

---

## 🎯 Core Objectives

- Provide a clean, unified interface for managing all areas of personal development.
- Help users build discipline and healthy habits.
- Make progress visible through analytics, history, and reflection.
- Assist with overcoming harmful behaviors and addictions.
- Use AI as a guide for journaling, insights, prompts, and pattern detection.
- Remove the need for multiple self-help apps.

---

## 🚀 MVP Features

### **✔ Tasks**
- Create, complete, organize tasks  
- Link tasks to goals  
- Daily/weekly view

### **✔ Habits**
- Habit creation  
- Streak tracking  
- Progress calendar  

### **✔ Addiction Tracking**
- Log urges, relapses, triggers  
- Statistics and patterns  

### **✔ Goals**
- Weekly / monthly / yearly goals  
- Progress overview  
- Link tasks and habits to goals  

### **✔ Journal**
- Daily notes  
- Mood / events logging  
- AI-assisted reflection prompts  

### **✔ AI Assistant**
- Suggest reflection questions  
- Provide summaries / insights  
- Help users stay aware and consistent  

---

## 🛠️ Technology Stack (planned)

- **Backend:** .NET Web API  
- **Database:** PostgreSQL  
- **Frontend:** Vue.js 
- **AI Layer:** API-based LLM integration (later models can be swapped)  
- **Deployment:** Docker + VPS + GitHub Actions  

### Локальная разработка

Перед первым запуском задайте секреты локально (User Secrets), см. [Backend/SECRETS.md](Backend/SECRETS.md).

Из корня репозитория одной командой можно запустить и бэкенд, и фронтенд:

```bash
npm run dev
```

Бэкенд (API) и фронтенд (Vite) запустятся параллельно. Остановка — `Ctrl+C` в том же терминале (остановятся оба процесса).

Отдельно:
- `npm run dev:backend` — только бэкенд
- `npm run dev:frontend` — только фронтенд

После изменений моделей EF примените миграции (остановите бэкенд, если он держит файлы сборки):

```bash
cd Backend
dotnet ef database update --project LifeHub-Backend.csproj
```

Если в SQLite уже есть колонка `Note` у `AddictionResets` (старая тестовая миграция), перед `database update` удалите её:  
`ALTER TABLE AddictionResets DROP COLUMN Note;` (SQLite 3.35+).

Для SQLite в миграции `AddictionResets.JournalEntryId` **не создаётся ограничение FOREIGN KEY** в БД (ограничение провайдера EF); связь задаётся только в модели приложения.

#### Зависание на `Acquiring an exclusive lock` / `__EFMigrationsLock`

В EF Core 9+ для SQLite миграции защищаются «блокировкой» через таблицу `__EFMigrationsLock`. Если миграция оборвалась (процесс убит, сбой) или одновременно открыт тот же файл БД, строка в этой таблице может остаться — тогда `dotnet ef database update` снова и снова выполняет `INSERT OR IGNORE` и **не продвигается**. Официально: [Concurrent migrations protection (SQLite)](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations#concurrent-migrations-protection).

**Что сделать:**

1. Остановите бэкенд и любые программы, которые держат `lifehub.db` открытым.
2. Удалите таблицу блокировки (путь к файлу совпадает с рантаймом: обычно `Backend/bin/Debug/net10.0/../database/lifehub.db` → `Backend/bin/Debug/database/lifehub.db`):

```bash
sqlite3 Backend/bin/Debug/database/lifehub.db "DROP TABLE IF EXISTS \"__EFMigrationsLock\";"
```

Без установленного `sqlite3` (типично для Windows) можно из корня репозитория:

```bash
python Backend/scripts/drop_migrations_lock.py
```

(Необязательный аргумент — полный путь к `.db`, если файл не в `Backend/bin/Debug/database/lifehub.db`.)

(Если у вас другой путь к БД — укажите его; при необходимости задайте тот же файл, что и в `Program.cs`, через `--connection` у `dotnet ef database update`.)

3. Повторите `dotnet ef database update`.

Затем при необходимости обновите клиентский OpenAPI: `npm run api:gen` из папки `Frontend` (бэкенд должен отдавать `/openapi/v1.json`).

---

## 📐 Architecture (draft)

LifeHub follows a modular design with clear domains:

- `Tasks`  
- `Habits`  
- `Journal`  
- `AddictionTracker`  
- `Goals`  
- `AI`  

Each module provides its own API layer, business logic, and database structures.  
AI is abstracted through a provider interface, allowing switching between local and cloud models.

---

## 🧭 Roadmap

### **Phase 1 — Documentation**
- Product vision and goals  
- Competitor analysis  
- MVP specification  
- Initial architectural plan  

### **Phase 2 — Prototyping**
- Wireframes and screens  
- User flows  
- OpenAPI specification  
- Finalize stack choices  

### **Phase 3 — MVP Implementation**
- Auth & user management  
- Tasks, habits, goals, journal modules  
- Basic AI integration  
- Analytics events  
- Production deployment  

### **Phase 4 — Iterative Development**
- Pattern detection  
- Advanced analytics  
- Cross-platform clients  
- Offline mode  
- Better AI guidance  
- Premium features  

---

## 📄 License

To be defined.

---

## 🤝 Contributing

LifeHub is currently an early-stage personal project.  
Contributions, ideas, and feedback are welcome — feel free to open issues or discussions.
