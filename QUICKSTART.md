# ?? Быстрый старт ProSushi Messenger

## 1?? **Запуск Backend**

```bash
cd prosushimsg
dotnet run --configuration Development
```

**Результат:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

? Серверек на `http://localhost:5000`
? SQLite база в `prosushi.db`
? SignalR Hub на `ws://localhost:5000/chathub`

---

## 2?? **Запуск Frontend (Blazor WASM)**

```bash
cd ProSushiMsg.Client
dotnet watch run
```

**Результат:**
```
Build succeeded.
Launching browser at http://localhost:5001
Hot reload enabled. For a list of supported edits, see https://aka.ms/dotnet/hot-reload
```

? Приложение на `http://localhost:5001`
? Автоматическая перезагрузка при изменениях

---

## 3?? **Тестирование**

### A. Регистрация

**Frontend:** Нажми на "Зарегистрируйся" в Login.razor

Или через **Postman**:
```
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "username": "alice",
  "email": "alice@example.com",
  "password": "SecurePass123!"
}
```

### B. Вход

**Frontend:** Введи `alice` / `SecurePass123!`

Или через **Postman**:
```
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "username": "alice",
  "password": "SecurePass123!"
}

Response:
{
  "token": "eyJ0eXAiOiJKV1QiLCJhbGc...",
  "userId": 1
}
```

### C. Реал-тайм чат (SignalR)

```javascript
// В консоли браузера:
const token = localStorage.getItem('auth_token');
const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/chathub?access_token=' + token)
    .withAutomaticReconnect()
    .build();

connection.on('ReceiveMessage', (userId, userName, message) => {
    console.log(`?? ${userName}: ${message}`);
});

connection.start().catch(err => console.error(err));

// Отправить сообщение
connection.invoke('SendMessage', 'Привет!');
```

---

## ??? Структура проекта

```
prosushimsg/                         ? Backend (ASP.NET Core)
??? Controllers/                     ? REST API endpoints
??? Hubs/                           ? SignalR Hub
??? Services/                       ? Бизнес-логика
??? Models/                         ? Entities (User, Message, Group)
??? Data/                           ? DbContext, Миграции
??? Program.cs                      ? Конфигурация
??? prosushi.db                     ? SQLite база

ProSushiMsg.Client/                  ? Frontend (Blazor WASM)
??? Pages/                          ? Razor компоненты
?   ??? Login.razor                 ? Вход/регистрация
?   ??? Chats.razor                 ? Главный чат UI
??? Services/                       ? Сервисы
?   ??? AuthService.cs              ? JWT токены
?   ??? ChatService.cs              ? HTTP API клиент
?   ??? SignalRService.cs           ? Реал-тайм
?   ??? EncryptionService.cs        ? E2EE
??? wwwroot/                        ? Static файлы
?   ??? service-worker.js           ? PWA
?   ??? manifest.json               ? PWA метаданные
??? Program.cs                      ? Инициализация
```

---

## ?? Чек-лист перед запуском

- [ ] Установлен .NET 10
- [ ] Visual Studio или VS Code с C# расширением
- [ ] Порты 5000 и 5001 свободны
- [ ] Git клонирован: `git clone ... && cd prosushimsg`
- [ ] Dependencies восстановлены: `dotnet restore`

---

## ?? Если что-то не работает

### Backend не запускается

```bash
# Очиста кэша
dotnet clean
dotnet restore
dotnet run
```

### Frontend ошибка "Cannot find module"

```bash
cd ProSushiMsg.Client
dotnet clean
dotnet restore
dotnet watch run
```

### SignalR не подключается

1. Проверь, что backend на `http://localhost:5000`
2. Посмотри в браузере DevTools ? Network ? WS
3. Проверь токен в LocalStorage: `localStorage.getItem('auth_token')`

### SQLite ошибка

```bash
# Удали старую базу и мигрируй заново
rm prosushi.db
cd prosushimsg
dotnet ef database update
```

---

## ?? Полезные команды

```bash
# Backend

# Запуск в режиме разработки
dotnet run

# Hot reload
dotnet watch run

# Просмотр логов
dotnet run --loglevel=Debug

# Миграции БД
dotnet ef migrations add AddNewTable
dotnet ef database update

# Очистка
dotnet clean

# Frontend

# Development с автоперезагрузкой
dotnet watch run

# Production build
dotnet publish -c Release

# Исправить ошибки формата
dotnet format

# Анализ кода
dotnet analyzers run
```

---

## ?? Использование

### 1. Открой браузер

```
http://localhost:5001
```

### 2. Зарегистрируйся

```
Username: alice
Email: alice@example.com
Password: SecurePass123!
```

### 3. Вход

```
Username: alice
Password: SecurePass123!
```

### 4. Перейди на страницу Chats

```
Ты увидишь интерфейс с чатами
(пока пусто — нужно создать второго пользователя)
```

### 5. Создай второго пользователя (второй браузер/инкогнито)

```
Username: bob
Email: bob@example.com
Password: SecurePass123!
```

### 6. Чат между alice и bob

Оба смогут видеть друг друга в реал-тайме через SignalR! ??

---

## ?? Безопасность (важно!)

?? **Перед деплоем:**

1. Замени `SecureKey` в `appsettings.json` на случайную строку (32 символа)
2. Включи HTTPS
3. Установи правильный CORS (не `AllowAnyOrigin`)
4. Используй HttpOnly cookies для JWT (вместо localStorage)
5. Обнови пакеты:
   ```bash
   dotnet add package System.IdentityModel.Tokens.Jwt --version 7.0.3
   ```

---

## ?? Дальше

- [ ] **Интегрировать E2EE** (см. `INTEGRATION_GUIDE.md`)
- [ ] **Добавить голосовые сообщения** (Yandex SpeechKit)
- [ ] **Группы** (chat rooms)
- [ ] **Загрузка файлов** (фото)
- [ ] **Деплой на VPS** (Docker, Nginx)
- [ ] **Мобильный клиент** (MAUI)

---

## ?? Что делать дальше?

**Выбери один вариант:**

1. **Тестирование** — создай 2-3 аккаунта, проверь чаты
2. **Интеграция E2EE** — скопируй код из `INTEGRATION_GUIDE.md`
3. **Голосовые сообщения** — добавь Yandex SpeechKit API
4. **Группы** — расширь ChatService для групповых чатов
5. **Деплой** — настрой Docker контейнер

**Что выбираешь?** ??
