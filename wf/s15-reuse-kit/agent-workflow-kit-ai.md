# Agent Workflow Kit — индекс

> **Назначение**: Сводный индекс элементов workflow kit для сценариев debug & fix.
> **Версия**: 1.0 | **Дата**: 2026-05-07
> **Проект**: TeachAssist (StudyFlow)

---

## Сценарии kit

| Сценарий | Режим автономности | Что запускать | Как принять | Когда остановиться |
|----------|-------------------|---------------|-------------|-------------------|
| Debugging brief | read-first | `.opencode/templates/template-debug-brief.md` | Гипотезы сформулированы, план содержит конкретные шаги, есть список проверок | Агент предлагает fix без установленной первопричины |
| Bugfix checklist | read-only | `.opencode/checklists/checklist-results.md` | Все пункты чеклиста отмечены, build/test/lint зелёные | Нарушены ограничения AGENTS.md (migrations, API contracts) |
| Bugfix + verify | read-first | `.opencode/skills/bugfix-n-verify/SKILL.md` | Первопричина исправлена, релевантные тесты проходят, diff-отчёт подготовлен | Выход за пределы границы задачи, изменения в миграциях |
| Review AI-diff | read-only / local diff | `wf/s14-simple-reuse/command-diff-review.md` | Блокирующие замечания перечислены отдельно, stat diff совпадает с целью | Выход за пределы границы задачи, несогласованные изменения API |

---

## Переносимость элементов

### Переносимые между проектами (generic)

| Элемент | Путь | Почему переносим |
|---------|------|-----------------|
| Debug brief template | `.opencode/templates/template-debug-brief.md` | Универсальная структура: проблема → контекст → гипотеза → план → проверки. Не зависит от стека |
| Bugfix + verify skill | `.opencode/skills/bugfix-n-verify/SKILL.md` | Общий паттерн: анализ → гипотезы → исправление → тесты → отчёт. Адаптируется под любой проект с тестами |
| Diff review command | `wf/s14-simple-reuse/command-diff-review.md` | Базовые git-команды универсальны; специфичные фильтры (migrations, секреты) настраиваются |

### Привязанные к StudyFlow (TeachAssist)

| Элемент | Путь | Почему привязан |
|---------|------|----------------|
| Acceptance checklist | `.opencode/checklists/checklist-results.md` | Содержит специфичные проверки: `dotnet build`, `dotnet test`, `npm run lint`, Vuetify 3 API, InMemory database, формат JSON-ответов |
| Autonomy map | `wf/s10-amap/autonomy-map-fixed.md` | Описывает сценарии, специфичные для StudyFlow: backend API, frontend VueJS/Vuetify, PostgreSQL миграции, xUnit тесты |
| Reusable workflows | `wf/s11-reuse/reusable-workflows.md` | Сценарии W1-W7 привязаны к стеку TeachAssist: ASP.NET Core, VueJS, EF Core, xUnit |

---

## Project rules

Постоянные правила проекта находятся в:

- **`AGENTS.md`** (корень проекта) — базовый контекст, стек, ограничения, рабочий процесс, формат ответа
- **`wf/s10-amap/autonomy-map-fixed.md`** — карта границ автономности, стоп-сигналы для всех сценариев

Не дублировать их содержимое в kit. Kit ссылается на них, но не заменяет.

---

## Подтверждение запуска

Kit был проверен агентом по запросу из `wf/s15-reuse-kit/work.md`:

> «Проверь мой agent workflow kit. Найди: дублирование, элементы без сценария, широкие команды, skills без стоп-сигналов, чеклисты без проверки результата, места для раннего возврата человека.»

Результат проверки: зафиксирован в `wf/s15-reuse-kit/work.md` (раздел «Проверка kit агентом»).

---

## Связанные документы

- `wf/s11-reuse/reusable-workflows.md` — каталог повторяющихся сценариев (W1-W7)
- `wf/s10-amap/autonomy-map-fixed.md` — карта границ автономности (S1-S7)
- `wf/s15-reuse-kit/work.md` — план создания kit и результаты проверки
- `AGENTS.md` — ограничения проекта
