# AlisaPlayFilm

Веб-приложение для взаимодействия с Яндекс Алисой, которое позволяет искать фильмы на Кинопоиске и Рутубе и открывать их в браузере.

## Архитектура

Проект построен на принципах чистой архитектуры (Clean Architecture) и состоит из следующих слоев:

- **Core** - доменные сущности, интерфейсы и перечисления
- **Application** - бизнес-логика, сервисы приложения, DTO
- **Infrastructure** - реализация интерфейсов (поиск фильмов, открытие браузера)
- **Server** - веб-API, контроллеры, конфигурация
- **Tray.App** - приложение на ETO.Forms, которое позволяет запускать сервер в фоне

## Функциональность

1. Приложение принимает запросы от Яндекс Алисы через вебхук
2. Извлекает название фильма из команды пользователя
3. Ищет фильм сначала на Кинопоиске, затем на Рутубе
4. Открывает найденный фильм в браузере
5. Отвечает Алисе о результате операции

## Команды Алисы

- "Алиса, включи фильм [название]"
- "Алиса, покажи фильм [название]"
- "Алиса, найди фильм [название]"
- "Алиса, открой фильм [название]"

## Требования

- **Windows, Linux или macOS** (x64)
- **Браузер** для открытия фильмов
- **.NET 9.0 Runtime** (не требуется для релизных бинарников — они self-contained)

> **Примечание:** Для разработки требуется .NET 9.0 SDK

## Установка

### Из релизов (рекомендуется)

1. Перейдите на страницу [Releases](https://github.com/Miclell/AlisaPlayFilm/releases)
2. Скачайте бинарник для вашей ОС:
   - **Windows:** `AlisaPlayFilm-Windows-x64.exe`
   - **Linux:** `AlisaPlayFilm-Linux-x64`
   - **macOS:** `AlisaPlayFilm-macOS-x64`

3. **Windows:** Просто запустите `.exe` файл
4. **Linux/macOS:** Сделайте файл исполняемым и запустите:
   ```bash
   chmod +x AlisaPlayFilm-Linux-x64
   ./AlisaPlayFilm-Linux-x64
   ```

Бинарники являются **single-file** и **self-contained** — не требуют установки .NET Runtime и содержат все зависимости.

### Из исходников

```bash
git clone https://github.com/your-username/AlisaPlayFilm.git
cd AlisaPlayFilm
dotnet build
```

## Запуск

### С GUI (Tray App) — рекомендуется

Приложение запускается в фоновом режиме с иконкой в системном трее:

- **Windows:** Запустите `AlisaPlayFilm.exe` — приложение появится в трее
- **Linux:** Запустите бинарник — появится иконка в системном трее (требуется GTK)
- **macOS:** Запустите бинарник — появится иконка в строке меню

**Возможности:**
- Управление сервером через контекстное меню трея
- Просмотр логов через веб-интерфейс
- Уведомления о статусе сервера
- Автоматический запуск при старте системы (настраивается в меню)

### Без GUI (только сервер)

Для запуска только веб-сервера без GUI:

```bash
dotnet run --project src/Server/Server.csproj
```

Или используйте собранный бинарник Server (если собран отдельно).

**По умолчанию приложение доступно:**
- HTTP: `http://localhost:8080`
- HTTPS: `https://localhost:8980`
- Swagger UI: `http://localhost:8080/` или `https://localhost:8980/`
- Логи: `http://localhost:8080/api/logs` или `https://localhost:8980/api/logs`

## Конфигурация

Базовые настройки вшиты в бинарник, но при первом запуске автоматически создаются пользовательские файлы конфигурации, которые можно редактировать.

### Расположение конфигурационных файлов

- **Windows:** `%AppData%\AlisaPlayFilm\appsettings.json`
- **Linux:** `${XDG_CONFIG_HOME:-~/.config}/AlisaPlayFilm/appsettings.json`
- **macOS:** `~/Library/Application Support/AlisaPlayFilm/appsettings.json`

### Настройка портов

Откройте файл `appsettings.json` в пользовательской директории и измените порты:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:8080"
      },
      "Https": {
        "Url": "https://0.0.0.0:8980"
      }
    }
  }
}
```

> **Важно:** Приложение автоматически перечитывает конфигурацию при изменении файла (hot reload).

### Настройка логирования

Уровни логирования настраиваются в том же файле:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

Для режима разработки создайте `appsettings.Development.json` с более детальным логированием.  

## Настройка вебхука Яндекс Алисы

### Требования

- Приложение должно быть доступно из интернета по HTTPS
- Нужен валидный SSL-сертификат (самоподписанный сертификат подходит — Яндекс проверяет только наличие HTTPS)

### Шаги настройки

1. **Запустите приложение** на вашем компьютере или сервере
2. **Пробросьте порты** на роутере (если приложение на домашнем компьютере):
   - Порт 8080 (HTTP) — опционально
   - Порт 8980 (HTTPS) — обязательно
3. **Настройте вебхук** в навыке Яндекс Алисы:
   - Endpoint: `https://your-server.com:8980/api/alice`
   - Или используйте доменное имя, если оно настроено
4. **Проверьте доступность** — убедитесь, что endpoint доступен из интернета

### Альтернативные варианты для домашнего использования

Если у вас нет статического IP или не хотите пробрасывать порты:

- Используйте **туннели** (Cloudflare Tunnel, Ngrok, LocalTunnel) для создания публичного HTTPS URL

## API Endpoints

### POST /api/alice

Принимает запросы от Яндекс Алисы в формате [Alice API](https://yandex.ru/dev/dialogs/alice/doc/request.html).

**Пример запроса:**
```json
{
  "request": {
    "command": "включи фильм матрица",
    "original_utterance": "Алиса, включи фильм матрица",
    "type": "SimpleUtterance"
  },
  "session": {
    "session_id": "123",
    "user_id": "456",
    "message_id": 1
  },
  "version": "1.0"
}
```

**Пример ответа:**
```json
{
  "response": {
    "text": "Открываю фильм: Матрица (фильм, 1999)",
    "end_session": true
  },
  "version": "1.0",
  "session": {
    "session_id": "123",
    "user_id": "125",
    "message_id": 0
  }
}
```

## Просмотр логов

Приложение предоставляет веб-интерфейс для просмотра логов в реальном времени:

- **HTML интерфейс:** `http://localhost:8080/api/logs`
- **SSE поток:** `http://localhost:8080/api/logs/stream`
- **Экспорт:** `http://localhost:8080/api/logs/export?maxCount=1000`

Логи также можно настроить через `appsettings.json` (см. раздел [Конфигурация](#конфигурация)).

## Дополнительные возможности

### Веб-интерфейс

- **Swagger UI:** Доступен на корневом пути `/` для тестирования API
- **Логи:** Просмотр логов в реальном времени через `/api/logs`
- **Экспорт логов:** Скачивание логов в текстовом формате

### Системный трей (Tray App)

При использовании GUI версии доступны:

- Контекстное меню с управлением сервером
- Уведомления о статусе (старт/остановка/ошибки)
- Быстрый доступ к логам и настройкам
- Автозапуск при старте системы

## Структура проекта

```
AlisaPlayFilm/
├── src/
│   ├── Core/              # Доменный слой (сущности, интерфейсы)
│   ├── Application/       # Слой приложения (бизнес-логика, сервисы)
│   ├── Infrastructure/    # Слой инфраструктуры (поиск, браузер, логирование)
│   ├── Server/            # Веб-сервер (контроллеры, конфигурация)
│   └── Tray.App/          # GUI приложение (трей-индикатор)
└── README.md
```

## Разработка

### Требования для разработки

- .NET 9.0 SDK

### Сборка проекта

```bash
# Восстановление зависимостей
dotnet restore

# Сборка всех проектов
dotnet build

# Запуск в режиме разработки
dotnet run --project src/Tray.App/Tray.App.csproj
```

### Публикация релизных бинарников

```bash
# Windows
dotnet publish src/Tray.App/Tray.App.csproj \
  -c Release -r win-x64 \
  -p:TargetFrameworkOverride=net9.0-windows \
  -p:PlatformPackage=Eto.Platform.Windows \
  -p:EtoVersion=2.10.2 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o publish/win-x64

# Linux
dotnet publish src/Tray.App/Tray.App.csproj \
  -c Release -r linux-x64 \
  -p:TargetFrameworkOverride=net9.0 \
  -p:PlatformPackage=Eto.Platform.Gtk \
  -p:EtoVersion=2.10.2 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o publish/linux-x64

# macOS
dotnet publish src/Tray.App/Tray.App.csproj \
  -c Release -r osx-x64 \
  -p:TargetFrameworkOverride=net9.0 \
  -p:PlatformPackage=Eto.Platform.Mac64 \
  -p:EtoVersion=2.10.2 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o publish/osx-x64
```

### Расширение функциональности

#### Добавление нового источника поиска

1. Создайте класс, реализующий `IFilmSearchService` в проекте `Infrastructure`
2. Зарегистрируйте сервис в `Infrastructure/DependencyInjection.cs`
3. Обновите enum `SearchSource` в проекте `Core`

#### Изменение логики извлечения названия фильма

Логика извлечения названия фильма находится в `Application/Services/AliceService.ExtractFilmName()`. Вы можете добавить дополнительные ключевые слова или изменить алгоритм извлечения.

#### Добавление новых API endpoints

Создайте новый контроллер в `Server/Controllers/` и зарегистрируйте маршруты в `Startup.cs`.

## Лицензия

MIT — см. [LICENSE](./LICENSE).

