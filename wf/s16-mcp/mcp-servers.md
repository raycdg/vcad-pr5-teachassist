# MCP Contracts

Контракты подключенный mcp-серверов проекта

## Filesystem

**Создан**: 11.05.2026

**Тип подключения**: local

**Транспорт**: stdio

**Кодманда запуска**: 
```bash
npx -y @modelcontextprotocol/server-filesystem "path1" "path2" ...
```

**Используется**: в задачах чтения/записи/изменения файлов за пределами каталога проекта, а также получения списка файлов/каталогов, информации и файлах и т.п.

**Не используется**: в задачах, не связаннных с чтением/записью/изменением файлов

**Секреты**: нет

**Инструменты**: MCP filesystem предоставляет следующие инструменты

| Инструмент | Описание |
|------------|----------|
| `filesystem_list_allowed_directories` | Список разрешённых директорий |
| `filesystem_create_directory` | Создать директорию |
| `filesystem_directory_tree` | Дерево файлов/директорий |
| `filesystem_edit_file` | Редактировать файл (построчно) |
| `filesystem_get_file_info` | Метаданные файла |
| `filesystem_list_directory` | Список файлов в директории |
| `filesystem_list_directory_with_sizes` | Список с размерами файлов |
| `filesystem_move_file` | Переместить/переименовать файл |
| `filesystem_read_file` | Читать файл (устарел) |
| `filesystem_read_media_file` | Читать медиафайл (base64) |
| `filesystem_read_multiple_files` | Читать несколько файлов |
| `filesystem_read_text_file` | Читать текстовый файл |
| `filesystem_search_files` | Поиск файлов по паттерну |
| `filesystem_write_file` | Записать файл |

**Тип операций**: Read + Write

**Область видимости**: внешнее директории (из списка разрешенных, filesystem_list_allowed_directories)

**Уровень риска**: Read - Низкий, Write - Средний

 **Stop-point**: не требуется на данный момент
