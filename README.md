# ?? ProSushi Messenger — Полный Stack

**Корпоративный реал-тайм мессенджер на C# / .NET 10 + Blazor WASM**

## ? Что сделано

| Компонент | Статус | Описание |
|-----------|--------|---------|
| **Backend** | ? | ASP.NET Core 10 + SignalR + EF Core + SQLite |
| **Frontend** | ? | Blazor WebAssembly (WASM) + Razor компоненты |
| **RealtTime** | ? | SignalR Hub для мгновенных сообщений |
| **Auth** | ? | JWT токены + JWT Bearer |
| **E2EE** | ? | libsodium (NaCl) для шифрования |
| **PWA** | ? | Service Worker + offline режим |
| **UI/UX** | ? | Responsive дизайн + тёмная тема |
| **Database** | ? | SQLite (легко мигрировать на PostgreSQL) |

---

## ?? Ключевые фичи

### ? Реализовано
- 1-на-1 чаты
- Реал-тайм сообщения (SignalR)
- Статус онлайн/оффлайн
- JWT авторизация
- E2EE шифрование (Sodium)
- PWA (offline-first)
- Responsive UI
- Dark mode

### ?? В разработке
- Голосовые сообщения (Yandex SpeechKit)
- Загрузка фото/файлов
- Групповые чаты
- Расчёт заказов (интеграция с ботом)

### ? Планируется
- MAUI клиент (мобильные)
- Cloudflare Tunnel / Hysteria2 (обход блокировок)
- Push нотификации (FCM/APNs)
- Двойная аутентификация (2FA)

---

## ?? Архитектура

```
???????????????????????????????????????????????????
?  Blazor WASM App (UI)                           ?
?  - Pages: Login.razor, Chats.razor              ?
?  - Services: Auth, Chat, SignalR, Encryption   ?
???????????????????????????????????????????????????
                 ? HTTP + WebSocket
                 ?
???????????????????????????????????????????????????
?  ASP.NET Core 10 Backend                        ?
?  - Controllers: Auth, Messages, Users, Files    ?
?  - Hubs: ChatHub (SignalR)                      ?
?  - Services: JwtService, Crypto                 ?
?  - Database: AppDbContext (EF Core)             ?
???????????????????????????????????????????????????
                 ?
                 ?
         ????????????????
         ?  SQLite DB   ?
         ? prosushi.db  ?
         ????????????????
```

---

## ?? Быстрый старт

### Prerequisites
- **.NET 10** (или выше)
- **Visual Studio 2022** / **VS Code**
- Порты **5000** (backend) и **5001** (frontend)

### 1. Клонирование

```bash
git clone <repo>
cd prosushimsg
```

### 2. Запуск Backend

```bash
dotnet run --configuration Development
```

Откроется на `http://localhost:5000`

### 3. Запуск Frontend

В **отдельном терминале**:

```bash
cd ProSushiMsg.Client
dotnet watch run
```

Откроется на `http://localhost:5001`

### 4. Тестирование

1. Зарегистрируй двух пользователей (alice + bob)
2. Войди как alice
3. Вторая вкладка: Войди как bob
4. Отправляй сообщения в реал-тайм! ??

**Подробнее:** `QUICKSTART.md`

---

## ?? Структура проекта

### Backend (`prosushimsg/`)

```
prosushimsg/
??? Controllers/                ? API endpoints
?   ??? AuthController.cs       ? /api/auth/* (login, register)
?   ??? MessagesController.cs   ? /api/messages/* (history)
?   ??? UsersController.cs      ? /api/users/* (profile)
?   ??? GroupsController.cs     ? /api/groups/* (team rooms)
?   ??? FilesController.cs      ? /api/files/* (upload)
??? Hubs/
?   ??? ChatHub.cs              ? SignalR (реал-тайм)
??? Models/
?   ??? User.cs                 ? Entity: Users
?   ??? Message.cs              ? Entity: Messages
?   ??? Group.cs                ? Entity: Groups
?   ??? GroupMember.cs          ? Junction table
??? Services/
?   ??? JwtService.cs           ? JWT generation
??? Data/
?   ??? AppDbContext.cs         ? EF Core DbContext
??? Migrations/                 ? Database migrations
??? Program.cs                  ? Конфигурация
??? appsettings.json            ? Settings
```

### Frontend (`ProSushiMsg.Client/`)

```
ProSushiMsg.Client/
??? Pages/
?   ??? Login.razor             ? Вход/регистрация
?   ??? Chats.razor             ? Главный интерфейс чатов
??? Services/
?   ??? AuthService.cs          ? JWT & Authorization
?   ??? ChatService.cs          ? HTTP API Client
?   ??? SignalRService.cs       ? WebSocket (RealtTime)
?   ??? EncryptionService.cs    ? E2EE (Sodium)
?   ??? LocalStorageService.cs  ? Browser Storage
?   ??? EncryptedSignalRExtensions.cs ? Helpers
??? wwwroot/
?   ??? index.html              ? Entry point
?   ??? service-worker.js       ? PWA / Offline
?   ??? manifest.json           ? PWA manifest
?   ??? app.css                 ? Global styles
?   ??? appsettings.json        ? Client config
??? App.razor                   ? Root component
??? Program.cs                  ? Bootstrap
??? ProSushiMsg.Client.csproj   ? Project file
```

---

## ?? Безопасность

### ? Реализовано
- **JWT авторизация** (24ч expiry)
- **E2EE шифрование** (libsodium SealedBox)
- **Подписи сообщений** (PublicKeyAuth)
- **CORS конфигурация**
- **SQL Injection protection** (EF Core параметры)

### ?? Нужно сделать перед продакшеном
1. Включи **HTTPS** (обязательно!)
2. Используй **HttpOnly cookies** для JWT (вместо localStorage)
3. Установи правильный **CORS** (не `AllowAnyOrigin`)
4. Обнови пакеты (нужна версия >7.0.3):
   ```bash
   dotnet package update
   ```
5. Защити **appsettings.Production.json** (DB пароли, ключи)

---

## ??? Конфигурация

### appsettings.json (Backend)

```json
{
  "Jwt": {
    "SecureKey": "your-secret-key-32-symbols-minimum!!!",
    "ExpirationHours": 24
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=prosushi.db"
  },
  "FileUpload": {
    "MaxFileSizeBytes": 10485760,
    "AllowedExtensions": [".jpg", ".png", ".mp3", ".wav"]
  }
}
```

### appsettings.json (Frontend)

```json
{
  "ApiBaseAddress": "http://localhost:5000"
}
```

---

## ?? API Endpoints

### Auth
```
POST   /api/auth/register               Register user
POST   /api/auth/login                  Get JWT token
```

### Messages
```
GET    /api/messages/chats              Get chat list
GET    /api/messages/{chatId}           Get message history
GET    /api/messages/{chatId}?skip=0&take=50  Pagination
```

### Users
```
GET    /api/users/me                    Current user
GET    /api/users/{userId}              User profile
POST   /api/users/me/public-key         Update E2EE key
```

### Groups
```
GET    /api/groups                      Get all groups
POST   /api/groups                      Create group
GET    /api/groups/{groupId}            Get group
```

### Files
```
POST   /api/files/upload                Upload file
GET    /api/files/{fileId}              Download file
```

### SignalR Hub (`/chathub`)
```
SendMessage(message)                    Send 1-to-1
SendGroupMessage(groupId, message)      Send to group
UserTyping(groupId?)                    Typing indicator
MarkMessageAsRead(messageId)            Mark as read
ReceiveMessage(userId, userName, msg)   Receive (callback)
UserStatusChanged(userId, isOnline)     Status (callback)
```

---

## ?? Тестирование

### Unit Tests (рекомендуется)
```bash
# Создай проект
dotnet new xunit -n ProSushiMsg.Tests

# Tests
dotnet test
```

### Manual Testing (Postman)

Импортируй `ProSushiMsg.postman_collection.json`:
```bash
# В Postman: Import ? Paste raw
```

### E2E Tests (Playwright)
```bash
# Install
dotnet add package Microsoft.Playwright
pwsh bin/Debug/net10.0/playwright.ps1 install

# Run
dotnet test
```

---

## ?? Деплой

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10 AS runtime

WORKDIR /app
COPY bin/Release/net10.0/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["./prosushimsg"]
```

### VPS (512MB RAM)

1. **Build publish**
   ```bash
   dotnet publish -c Release --self-contained=false -r linux-x64
   ```

2. **Systemd service**
   ```ini
   [Unit]
   Description=ProSushi Messenger
   After=network.target

   [Service]
   Type=notify
   User=prosushi
   ExecStart=/opt/prosushi/prosushimsg
   Restart=on-failure
   Environment="ASPNETCORE_URLS=http://localhost:5000"

   [Install]
   WantedBy=multi-user.target
   ```

3. **Nginx reverse proxy**
   ```nginx
   server {
       listen 80;
       server_name messenger.yourdomain.com;

       location / {
           proxy_pass http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection "upgrade";
           proxy_buffering off;
       }

       # Static assets cache
       location ~* \.(?:css|js|png|jpg|gif|woff2)$ {
           add_header Cache-Control "max-age=31536000, immutable";
       }
   }
   ```

**Подробнее:** `README_BLAZOR.md`, `INTEGRATION_GUIDE.md`

---

## ?? Документация

| Файл | Содержание |
|------|-----------|
| `QUICKSTART.md` | ? Быстрый старт (5 минут) |
| `INTEGRATION_GUIDE.md` | ?? Интеграция backend + frontend |
| `README_BLAZOR.md` | ?? Blazor WASM + PWA документация |
| `ProSushiMsg.postman_collection.json` | ?? API тесты |

---

## ?? Примеры кода

### Авторизация (Frontend)

```csharp
@inject AuthService authService
@inject NavigationManager nav

private async Task Login()
{
    var (success, error) = await authService.LoginAsync("alice", "pass123");
    if (success)
        nav.NavigateTo("/chats");
    else
        errorMessage = error;
}
```

### SignalR (Frontend)

```csharp
@inject SignalRService signalR

protected override async Task OnInitializedAsync()
{
    await signalR.ConnectAsync();
    signalR.OnMessageReceived += (userId, userName, msg) => 
    {
        messages.Add(new MessageDto { 
            SenderId = userId, 
            Content = msg 
        });
        StateHasChanged();
    };
}

private async Task SendMessage()
{
    await signalR.SendMessageAsync(MessageInput, chatId);
}
```

### E2EE (Frontend)

```csharp
@inject EncryptionService encryption

var (publicKey, secretKey) = encryption.GenerateKeyPair();

// Send encrypted
var encrypted = encryption.EncryptMessage(message, recipientPublicKey);
await signalR.SendMessageAsync(encrypted);

// Receive encrypted
var decrypted = encryption.DecryptMessage(encrypted, senderPublicKey);
```

### API (Backend)

```csharp
[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    [HttpGet("{chatId}")]
    [Authorize]
    public async Task<IActionResult> GetMessages(int chatId, int skip = 0, int take = 50)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var messages = await _context.Messages
            .Where(m => (m.ReceiverId == userId || m.SenderId == userId) && m.Id == chatId)
            .OrderByDescending(m => m.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return Ok(messages);
    }
}
```

---

## ?? Troubleshooting

| Проблема | Решение |
|----------|---------|
| Port 5000 занят | `netstat -tulpn \| grep 5000` и убить процесс |
| SignalR не подключается | Проверь: Backend работает? Токен в localStorage? |
| SQLite ошибка | `rm prosushi.db && dotnet ef database update` |
| CSS не загружается | `dotnet clean && dotnet build` |
| E2EE ошибка | Проверь версию Sodium.Core (1.3.3) |

---

## ?? Что дальше?

### Следующие шаги (в порядке приоритета)

1. **Голосовые сообщения** ? Начни отсюда!
   - Интегрируй Yandex SpeechKit API
   - Записывай WAV, шифруй, отправляй

2. **Группы и каналы**
   - Расширь ChatService для групповых чатов
   - Групповое E2EE (Double Ratchet)

3. **Загрузка файлов**
   - Ужимай изображения на клиенте
   - Загружай зашифрованные файлы

4. **Расчёт заказов**
   - Интеграция с ботом
   - REST API для расчёта км, зарплаты

5. **MAUI клиент**
   - Переиспользуй сервисы (Services layer)
   - Собери для iOS/Android

6. **Деплой на VPS**
   - Docker контейнер
   - Systemd service
   - Nginx, SSL, etc.

---

## ?? Поддержка

- ?? **Документация:** `README_BLAZOR.md`, `INTEGRATION_GUIDE.md`
- ?? **Issues:** GitHub Issues
- ?? **Дискуссии:** GitHub Discussions

---

## ?? Лицензия

MIT License — используй свободно!

---

**Made with ?? & ?? by ProSushi Team**

**Версия:** 1.0.0-beta  
**.NET:** 10.0  
**Blazor:** WASM  
**Status:** ? Development-Ready

---

## ?? Get Started

```bash
# 1. Clone
git clone <repo> && cd prosushimsg

# 2. Run Backend
dotnet run &

# 3. Run Frontend (new terminal)
cd ProSushiMsg.Client && dotnet watch run

# 4. Open browser
http://localhost:5001
```

**Готово! Начни создавать чаты! ??**
