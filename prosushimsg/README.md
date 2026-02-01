# ?? ProSushiMsg — Корпоративный мессенджер

Простой, но мощный мессенджер для небольших команд (10-50 человек) на базе **.NET 8+**, **SignalR**, **SQLite** и **JWT**.

---

## ?? Быстрый старт

### 1. Запуск сервера
```bash
cd prosushimsg
dotnet run
```

Сервер запустится на `http://localhost:5000` (или порт, указанный в консоли).

---

## ?? API Endpoints

### ?? Авторизация

#### Регистрация
```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "ivan",
  "password": "12345",
  "fullName": "Иван Петров"
}
```

**Ответ:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 1,
  "username": "ivan"
}
```

#### Вход
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "ivan",
  "password": "12345"
}
```

---

### ?? Пользователи

**Важно:** Все эндпоинты требуют JWT токен в заголовке:
```
Authorization: Bearer <твой_токен>
```

#### Получить текущего пользователя
```http
GET /api/users/me
```

#### Список всех пользователей (для поиска)
```http
GET /api/users?search=иван
```

#### Онлайн пользователи
```http
GET /api/users/online
```

---

### ?? Сообщения

#### История личного чата
```http
GET /api/messages/direct/2?limit=50
```
*(Получить последние 50 сообщений с пользователем ID=2)*

#### История группового чата
```http
GET /api/messages/group/1?limit=50
```

#### Список диалогов (недавние чаты)
```http
GET /api/messages/conversations
```

#### Пометить как прочитанное
```http
POST /api/messages/mark-read
Content-Type: application/json

{
  "messageIds": [1, 2, 3]
}
```

#### Удалить сообщение
```http
DELETE /api/messages/5
```

---

### ?? Группы

#### Мои группы
```http
GET /api/groups
```

#### Создать группу
```http
POST /api/groups
Content-Type: application/json

{
  "name": "Команда курьеров"
}
```

#### Добавить участника (только админы)
```http
POST /api/groups/1/members
Content-Type: application/json

{
  "userId": 3
}
```

#### Участники группы
```http
GET /api/groups/1/members
```

#### Покинуть группу
```http
DELETE /api/groups/1/leave
```

---

### ?? Файлы

#### Загрузить файл (фото, голосовое, документ)
```http
POST /api/files/upload
Content-Type: multipart/form-data

file: <binary>
type: Photo (или Voice, File)
```

**Ответ:**
```json
{
  "fileUrl": "/api/files/abc123.jpg",
  "fileName": "abc123.jpg",
  "size": 245678,
  "type": "Photo"
}
```

#### Скачать файл
```http
GET /api/files/abc123.jpg
```

#### Удалить файл
```http
DELETE /api/files/abc123.jpg
```

---

## ?? SignalR (реал-тайм)

### Подключение

JavaScript клиент:
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/chathub", {
        accessTokenFactory: () => "твой_jwt_токен"
    })
    .build();

await connection.start();
```

.NET клиент:
```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/chathub", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult("твой_jwt_токен");
    })
    .Build();

await connection.StartAsync();
```

### События

#### Получить сообщение
```javascript
connection.on("ReceiveMessage", (message) => {
    console.log("Новое сообщение:", message);
});
```

#### Получить групповое сообщение
```javascript
connection.on("ReceiveGroupMessage", (message) => {
    console.log("Групповое сообщение:", message);
});
```

#### Статус пользователя изменился
```javascript
connection.on("UserStatusChanged", (userId, isOnline) => {
    console.log(`Пользователь ${userId} ${isOnline ? 'онлайн' : 'офлайн'}`);
});
```

### Отправка сообщений

#### Личное сообщение
```javascript
await connection.invoke("SendMessage", 2, "Привет!", 0); // 0 = Text
```

#### Групповое сообщение
```javascript
await connection.invoke("JoinGroup", 1); // Присоединиться к группе
await connection.invoke("SendGroupMessage", 1, "Привет всем!");
```

---

## ??? База данных

SQLite файл: `prosushi.db` (создаётся автоматически при первом запуске).

### Миграции (если нужны изменения)
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## ?? Деплой на VPS (512 МБ RAM)

### Вариант 1: Systemd Service
```bash
# Публикация
dotnet publish -c Release -o /opt/prosushimsg

# Создать сервис /etc/systemd/system/prosushimsg.service
[Unit]
Description=ProSushiMsg

[Service]
WorkingDirectory=/opt/prosushimsg
ExecStart=/usr/bin/dotnet /opt/prosushimsg/prosushimsg.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target

# Запуск
sudo systemctl enable prosushimsg
sudo systemctl start prosushimsg
```

### Вариант 2: Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "prosushimsg.dll"]
```

### Cloudflare Tunnel (обход блокировок)
```bash
# Установка cloudflared
wget https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64
chmod +x cloudflared-linux-amd64
sudo mv cloudflared-linux-amd64 /usr/local/bin/cloudflared

# Создать туннель
cloudflared tunnel login
cloudflared tunnel create prosushimsg
cloudflared tunnel route dns prosushimsg msg.yourdomain.com

# Конфиг ~/.cloudflared/config.yml
tunnel: <tunnel-id>
credentials-file: /root/.cloudflared/<tunnel-id>.json

ingress:
  - hostname: msg.yourdomain.com
    service: http://localhost:5000
  - service: http_status:404

# Запуск
cloudflared tunnel run prosushimsg
```

---

## ?? Безопасность (ВАЖНО!)

### Что сделать перед продакшеном:

1. **Изменить секретный ключ JWT** в `JwtService.cs`:
   ```csharp
   private const string SecretKey = "генерируй_случайную_строку_64_символа";
   ```

2. **Использовать BCrypt для паролей** вместо SHA256:
   ```bash
   dotnet add package BCrypt.Net-Next
   ```

3. **HTTPS только!** Получи сертификат через Let's Encrypt:
   ```bash
   sudo certbot --nginx -d msg.yourdomain.com
   ```

4. **Ограничение размера файлов** уже есть (10 МБ), но можно уменьшить.

5. **Rate limiting** для защиты от DDoS (пакет `AspNetCoreRateLimit`).

---

## ?? Тестирование через curl

### Регистрация и вход
```bash
# Регистрация
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"test","password":"12345","fullName":"Тестовый Юзер"}'

# Сохрани токен из ответа
TOKEN="eyJhbGciOiJI..."

# Получить себя
curl http://localhost:5000/api/users/me \
  -H "Authorization: Bearer $TOKEN"
```

### Загрузить фото
```bash
curl -X POST http://localhost:5000/api/files/upload \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@photo.jpg" \
  -F "type=Photo"
```

---

## ?? Следующие шаги

1. **Клиент MAUI** (Android/iOS)
2. **E2EE** (Signal Protocol на клиенте)
3. **Push уведомления** (FCM/APNs)
4. **Интеграция с ботом расчёта** (для курьеров)
5. **Голосовой ввод** (Yandex SpeechKit)

---

## ?? Проблемы?

- **База не создаётся:** Проверь права на запись в папке
- **SignalR не работает:** Включи WebSockets в nginx/IIS
- **512 МБ мало:** Уменьши лимиты файлов, добавь swap

---

**Сделано с ?? для небольших команд**
