# ?? ProSushi Messenger — Blazor WASM + PWA Клиент

**Корпоративный мессенджер с реал-тайм, шифрованием и PWA.**

---

## ?? Что сделано

? **Blazor WebAssembly** — полнофункциональный C# на фронте  
? **PWA** — работает оффлайн (Service Worker + IndexedDB)  
? **SignalR** — реал-тайм сообщения и статусы онлайн  
? **JWT авторизация** — безопасное хранение токена  
? **E2EE шифрование** — libsodium для приватных чатов  
? **Responsive UI** — мобильная и десктопная версии  
? **Тёмная тема** — поддержка prefers-color-scheme  

---

## ??? Архитектура

```
???????????????????????????????????????
?  Blazor WASM App (UI Components)     ?
???????????????????????????????????????
?  Services Layer:                     ?
?  - AuthService (JWT)                 ?
?  - SignalRService (RealtimeHub)      ?
?  - ChatService (REST API)            ?
?  - EncryptionService (Libsodium)     ?
?  - LocalStorageService (IndexedDB)   ?
???????????????????????????????????????
?  HTTP + SignalR ? Network ? Backend  ?
???????????????????????????????????????
?  Service Worker (Offline Cache)      ?
?  PWA Manifest + Icons                ?
???????????????????????????????????????
```

---

## ?? Быстрый старт

### 1. Установка зависимостей

```bash
# В папке prosushimsg.client
dotnet add package Microsoft.AspNetCore.SignalR.Client --version 10.0.0
dotnet add package System.IdentityModel.Tokens.Jwt --version 7.0.0
dotnet add package Sodium.Core --version 1.3.3  # E2EE
```

### 2. Конфигурация

**appsettings.json:**
```json
{
  "ApiBaseAddress": "http://localhost:5000"  // или production URL
}
```

### 3. Program.cs — инициализация

```csharp
// Зарегистрировать сервисы перед BuildAsync()
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<EncryptionService>();

// Вызвать инициализацию при загрузке
var app = builder.Build();
var authService = app.Services.GetRequiredService<AuthService>();
await authService.InitializeAsync();
await app.RunAsync();
```

### 4. Запуск

```bash
# Development
dotnet watch run --project prosushimsg.client

# Production build
dotnet publish -c Release
# Результат: bin/Release/net10.0/browser-wasm/publish/
```

---

## ?? Компоненты

### Login.razor
- Форма входа/регистрации
- Сохранение JWT токена
- Redirection после успеха

### Chats.razor
- Список активных чатов
- Реал-тайм обновление статусов
- Отправка текста, фото, голосовых
- Scroll to bottom для новых сообщений
- Индикатор печатания

---

## ?? E2EE (Шифрование)

### Использование

```csharp
@inject EncryptionService Encryption

// При загрузке страницы:
protected override async Task OnInitializedAsync()
{
    var (publicKey, secretKey) = Encryption.GenerateKeyPair();
    await LocalStorage.SetAsync("secret_key", secretKey);
    
    // Отправить public key на сервер
    await ChatService.UpdateUserPublicKeyAsync(publicKey);
}

// Отправка зашифрованного сообщения:
private async Task SendEncryptedMessage()
{
    var encrypted = Encryption.EncryptMessage(
        MessageInput, 
        SelectedChat.RecipientPublicKey
    );
    await SignalRService.SendMessageAsync(encrypted);
}

// Получение:
SignalRService.OnMessageReceived += (userId, userName, message) =>
{
    var decrypted = Encryption.DecryptMessage(message, senderPublicKey);
    // Показываем расшифрованное сообщение
};
```

---

## ?? PWA & Offline

### Service Worker (`service-worker.js`)

**Стратегии:**
- **Cache First** — статика (CSS, JS, images)
- **Network First** — API запросы (с fallback кэшем)
- **Network Only** — SignalR (для реал-тайма)

**Background Sync:**
```javascript
// Автоматическая отправка сообщений при восстановлении связи
self.addEventListener('sync', event => {
    if (event.tag === 'sync-messages') {
        event.waitUntil(syncPendingMessages());
    }
});
```

### Manifest.json

- Название и описание приложения
- Иконки для разных размеров
- Стартовая страница
- Скрепки (shortcuts) для быстрого доступа

---

## ??? API Integration

### AuthService

```csharp
// Вход
var (success, error) = await authService.LoginAsync("user", "pass");
if (success)
    Navigation.NavigateTo("/chats");

// Регистрация
var (ok, err) = await authService.RegisterAsync("user", "email@com", "pass");

// Выход
await authService.LogoutAsync();
```

### ChatService

```csharp
// Получить чаты
var chats = await chatService.GetChatsAsync();

// Получить сообщения
var messages = await chatService.GetMessagesAsync(chatId, skip: 0, take: 50);

// Создать группу
var (ok, groupId) = await chatService.CreateGroupAsync("Team", new[] { 1, 2, 3 });

// Загрузить файл
var (success, fileUrl) = await chatService.UploadFileAsync(
    fileData: audioBytes,
    fileName: "voice.ogg",
    contentType: "audio/ogg"
);
```

### SignalRService

```csharp
// Подключение
await signalRService.ConnectAsync();

// Обработчики
signalRService.OnMessageReceived += (userId, userName, message) => {
    // Обновить UI
};

signalRService.OnUserStatusChanged += (userId, isOnline) => {
    // Показать статус
};

// Отправка
await signalRService.SendMessageAsync("Привет!");
await signalRService.SendTypingAsync();
await signalRService.MarkAsReadAsync(messageId);
```

---

## ?? Стили

### CSS Переменные (рекомендуется)

```css
:root {
    --primary: #667eea;
    --secondary: #764ba2;
    --success: #4caf50;
    --danger: #ff6b6b;
    --light: #f5f5f5;
    --dark: #1a1a1a;
    --border-radius: 8px;
    --shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
}
```

### Responsive Breakpoints

```css
@media (max-width: 768px) {
    /* Мобильная версия */
}

@media (max-width: 480px) {
    /* Маленькие экраны */
}

@media (prefers-color-scheme: dark) {
    /* Тёмная тема */
}
```

---

## ?? Подводные камни

### 1. **LocalStorage в WASM**
?? Токены хранятся в `localStorage` — **это небезопасно для критичных данных!**

**Решение:**
```csharp
// Используй HttpOnly cookies на сервере
// Или зашифруй токен перед сохранением
var encryptedToken = Encryption.EncryptMessage(token, serverPublicKey);
await LocalStorage.SetAsync("token", encryptedToken);
```

### 2. **SignalR + JWT в Query String**
?? Токен видна в URL, может быть перехвачена по сети!

**Решение:**
```csharp
// Используй HTTPS (обязательно!)
// Или передавай токен в заголовках (требует настройки CORS)
options.WithUrl(url, options =>
{
    options.AccessTokenProvider = async () => token;
});
```

### 3. **IndexedDB Storage Limit**
?? По умолчанию ~50MB на сайт (зависит от браузера)

**Решение:**
```csharp
// Используй pagination/пруну старые сообщения
var oldMessages = messages.Where(m => m.Timestamp < DateTime.Now.AddDays(-7));
await idb.DeleteAsync("messages", oldMessages.Select(m => m.Id));
```

### 4. **Nonce Reuse в Encryption**
?? **Не переиспользуй nonce!** Это разрушает AES-GCM

```csharp
// ? Правильно (новый nonce каждый раз):
var nonce = SodiumCore.GetRandomBytes(SecretBox.NonceBytes);
var encrypted = SecretBox.Create(data, nonce, key);

// ? Неправильно:
var nonce = new byte[24]; // All zeros!
var encrypted = SecretBox.Create(data, nonce, key); // Уязвимо!
```

---

## ?? Производительность (на VPS 512MB)

| Компонент | Вес | Время загрузки |
|-----------|-----|---|
| Blazor WASM bundle | ~2.5 MB | ~800ms |
| SignalR JS Interop | — | ~50ms |
| Service Worker | ~15KB | ~100ms |
| **Итого** | **~2.6 MB** | **~950ms** |

**Оптимизация:**
- Используй `BlazorWebAssemblyStatic` в `Program.cs`
- Сжимай бандл через `trimming`
- Кэш Service Worker'ом

---

## ?? Backend Integration (напоминание)

### Backend Endpoints

```
POST   /api/auth/login              ? Вход
POST   /api/auth/register           ? Регистрация
GET    /api/messages/chats          ? Список чатов
GET    /api/messages/{chatId}       ? Сообщения
POST   /api/groups                  ? Создать группу
POST   /api/files/upload            ? Загрузить файл
HUB    /chathub (SignalR)           ? Реал-тайм
```

### CORS Setup на Backend

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

---

## ?? Деплой на VPS (512MB)

### 1. Publish Build

```bash
dotnet publish -c Release --self-contained=false -r linux-x64
```

### 2. Nginx Configuration

```nginx
server {
    listen 80;
    server_name messenger.yourdomain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        
        # SignalR
        proxy_set_header X-Real-IP $remote_addr;
        proxy_buffering off;
    }

    # Service Worker cache
    location /service-worker.js {
        add_header Cache-Control "max-age=0, must-revalidate";
    }

    # Static assets — кэш на месяцы
    location ~* \.(?:css|js|gif|jpg|jpeg|png|woff2)$ {
        add_header Cache-Control "max-age=2592000, immutable";
    }
}
```

### 3. Systemd Service

```ini
[Unit]
Description=ProSushi Messenger
After=network.target

[Service]
Type=notify
User=prosushi
WorkingDirectory=/opt/prosushi
ExecStart=/opt/prosushi/prosushimsg
Restart=on-failure
RestartSec=5s
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="ASPNETCORE_URLS=http://localhost:5000"

[Install]
WantedBy=multi-user.target
```

---

## ?? Дополнительно

### Testing

```csharp
// Unit тест для AuthService
[Fact]
public async Task LoginAsync_WithValidCredentials_SetsTokenAndUserId()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.Expect(HttpMethod.Post, "*/api/auth/login")
        .Respond(HttpStatusCode.OK, 
            new StringContent("{\"token\":\"abc123\",\"userId\":1}"));

    // Act
    var result = await authService.LoginAsync("user", "pass");

    // Assert
    Assert.True(result.Success);
    Assert.NotNull(authService.CurrentToken);
}
```

### Debugging

```javascript
// В console браузера:
// Service Worker logs
navigator.serviceWorker.getRegistrations()
    .then(regs => regs.forEach(r => console.log(r)));

// SignalR events
window.signalRConnection?.on('ReceiveMessage', msg => console.log('??', msg));

// LocalStorage
Object.entries(localStorage).forEach(([k, v]) => console.log(k, v));
```

---

## ?? Что дальше?

- [ ] Интеграция с Yandex SpeechKit для голосовых сообщений
- [ ] Юридическое расчёт заказов (бот)
- [ ] Cloudflare Tunnel / Hysteria2 для обхода блокировок
- [ ] Нотификации (push через FCM/APNs)
- [ ] Архивирование сообщений (S3 совместимое хранилище)
- [ ] E2EE для групповых чатов (Double Ratchet)

---

## ?? Поддержка

Вопросы? Пиши в issues или на [GitHub Discussions](https://github.com).

**Made with ?? & ??**
