# ?? Исправление "An unhandled error has occurred. Reload"

## ? Что исправили:

### 1. **Program.cs** — Регистрация сервисов
Добавили все необходимые сервисы в DI контейнер:
- `LocalStorageService`
- `AuthService`
- `ChatService`
- `SignalRService`
- `EncryptionService`

### 2. **App.razor** — Правильный Router
Исправили синтаксис `NotFound` компонента

### 3. **Index.razor** — Асинхронная инициализация
Добавили `await AuthService.InitializeAsync()` чтобы загрузить токен из localStorage перед редиректом

---

## ?? Перезапуск

### Остановить Frontend
Нажми **Ctrl+C** в терминале где запущен `dotnet watch`

### Запустить заново
```powershell
cd ProSushiMsg.Client
dotnet watch run
```

**Браузер откроется на http://localhost:5001**

---

## ?? Что должно произойти:

1. Загрузится спиннер (крутящаяся анимация)
2. Проверится наличие токена в localStorage
3. **Если токена нет:** ? редирект на `/login`
4. **Если токен есть:** ? редирект на `/chats`

---

## ? Тестирование:

### Первый запуск (нет токена)
```
http://localhost:5001
  ?
Spinner (100ms)
  ?
/login (Login.razor)
```

### После логина
```
Login ? AuthService.LoginAsync()
  ?
Токен сохраняется в localStorage
  ?
Navigation.NavigateTo("/chats")
  ?
Chats.razor
```

---

## ?? Проверка в DevTools (F12)

### Application ? Local Storage
```
auth_token: "eyJ0eXAiOiJKV1QiLCJhbGc..."
auth_user_id: 1
```

### Console
```
[SignalR] Connected to http://localhost:5000/chathub
```

---

## ? Если ошибка всё ещё есть:

### 1. Очисти кэш браузера
```
Ctrl+Shift+Delete ? Очистить всё ? Перезагрузить
```

### 2. Очисти localStorage
Открой DevTools (F12):
```javascript
localStorage.clear();
location.reload();
```

### 3. Очисти build кэш
```powershell
cd ProSushiMsg.Client
dotnet clean
dotnet build
dotnet watch run
```

### 4. Проверь Console в DevTools
Нажми **F12** ? **Console** и посмотри на ошибки:
- Красные ошибки = JavaScript exceptions
- Проверь Stack Trace

### 5. Проверь Network
**F12** ? **Network** ? перезагрузи страницу:
- `_framework/blazor.webassembly.js` — должен загрузиться (200 OK)
- `_framework/dotnet.wasm` — должен загрузиться (200 OK)
- `/api/...` запросы — проверь статусы

---

## ?? Частые ошибки:

### Ошибка: "Cannot read properties of null"
**Причина:** Сервис не зарегистрирован в Program.cs

**Решение:** Проверь что все сервисы добавлены:
```csharp
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();
// и т.д.
```

---

### Ошибка: "Failed to fetch"
**Причина:** Backend не запущен или неправильный URL

**Решение:**
1. Проверь что backend работает: `http://localhost:5000`
2. Проверь `wwwroot/appsettings.json`:
   ```json
   {
     "ApiBaseAddress": "http://localhost:5000"
   }
   ```

---

### Ошибка: "401 Unauthorized"
**Причина:** Токен невалидный или истёк

**Решение:**
```javascript
// В DevTools Console
localStorage.clear();
location.reload();
```

Потом заново залогинься.

---

## ?? Чек-лист:

- [ ] Backend запущен на `http://localhost:5000`
- [ ] Frontend запущен на `http://localhost:5001`
- [ ] Нет ошибок в Console (F12)
- [ ] Login.razor загружается
- [ ] После логина ? редирект на `/chats`
- [ ] SignalR подключён

---

## ?? Если ничего не помогло:

Откройти DevTools (F12) ? Console и скопируй **полный Stack Trace**:
```
Error: ...
    at ...
    at ...
```

Это поможет найти точную причину ошибки.

---

**? После исправлений всё должно работать! Тестируй! ??**
