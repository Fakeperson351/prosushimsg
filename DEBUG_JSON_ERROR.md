# ?? Отладка JSON ошибки "ExpectedStartOfValueNotFound"

## ?? Проблема
```
ManagedError: AggregateException_ctor_DefaultMessage 
(ExpectedStartOfValueNotFound, LineNumber: 0 | BytePositionInLine: 0.)
```

Эта ошибка означает что **HttpClient получил невалидный JSON** (пустую строку, HTML или ошибку).

---

## ?? Шаг 1: Проверь Backend

### Открой в браузере:
```
http://localhost:5000/api/auth/login
```

### ? Правильный ответ (405 Method Not Allowed):
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Method Not Allowed",
  "status": 405
}
```
**? Backend работает!**

### ? Неправильные ответы:

**"This site can't be reached"** ? Backend НЕ запущен
```powershell
cd prosushimsg
dotnet run
```

**"Connection refused"** ? Проверь порт 5000
```powershell
netstat -ano | findstr :5000
```

---

## ?? Шаг 2: Открой DevTools

### Перезапусти Frontend с логами:
```powershell
cd ProSushiMsg.Client
dotnet watch run
```

### Открой DevTools (F12):
1. **Console** ? посмотри логи
2. **Network** ? посмотри HTTP запросы

---

## ?? Шаг 3: Читай логи в Console

После перезагрузки страницы ты должен увидеть:

### ? Нормальный запуск:
```
?? Index.razor: Начало инициализации
?? localStorage.getItem: auth_token ? null
?? Index.razor: IsAuthenticated = false
?? Index.razor: Редирект на /login
```

### ? Если есть ошибка:
```
? Index.razor ERROR: Cannot read properties...
Stack: ...
```

**? Скопируй весь Stack Trace и посмотри на строку где падает**

---

## ?? Шаг 4: Network Tab

### F12 ? Network ? перезагрузи страницу

Посмотри на HTTP запросы:

#### Если видишь красные запросы:
1. **Клик на красный запрос**
2. **Headers** ? проверь URL
3. **Response** ? посмотри что вернул сервер

### Возможные проблемы:

#### CORS ошибка
```
Access to fetch at 'http://localhost:5000/...' from origin 'http://localhost:5001' 
has been blocked by CORS policy
```

**Решение:** Проверь `prosushimsg/Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5001", "https://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

#### 404 Not Found
Backend не найден endpoint.

**Решение:** Проверь что backend запущен и Controllers зарегистрированы.

#### 500 Internal Server Error
Backend упал.

**Решение:** Посмотри логи backend в терминале.

---

## ?? Шаг 5: Тест регистрации

### Открой Login страницу:
```
http://localhost:5001/login
```

### Введи данные:
```
Username: testuser
Password: Test123!
```

### Нажми "Войти" и смотри Console:

#### ? Успешный вход:
```
?? Login: Попытка входа пользователя 'testuser'
?? FETCH: http://localhost:5000/api/auth/login
? RESPONSE: http://localhost:5000/api/auth/login Status: 200
?? Login: Результат - Success=true, Error=null
? Login: Вход успешен, редирект на /chats
```

#### ? Ошибка:
```
?? Login: Попытка входа пользователя 'testuser'
?? FETCH: http://localhost:5000/api/auth/login
? FETCH ERROR: http://localhost:5000/api/auth/login TypeError: Failed to fetch
? Login EXCEPTION: Failed to fetch
```

**? Backend не отвечает!**

---

## ??? Шаг 6: Типичные решения

### Backend не запущен
```powershell
cd prosushimsg
dotnet run
```

### Порт 5000 занят
```powershell
netstat -ano | findstr :5000
taskkill /PID <PID> /F
dotnet run
```

### Очистить кэш браузера
```
Ctrl + Shift + Delete ? Очистить всё
```

### Очистить localStorage
```javascript
// В Console (F12)
localStorage.clear();
location.reload();
```

### Пересобрать проект
```powershell
cd ProSushiMsg.Client
dotnet clean
dotnet build
dotnet watch run
```

---

## ?? Чек-лист отладки

- [ ] Backend запущен (`http://localhost:5000/api/auth/login` отвечает)
- [ ] Frontend запущен (`http://localhost:5001`)
- [ ] DevTools открыт (F12)
- [ ] Console показывает логи `?? Index.razor: Начало...`
- [ ] Network не показывает красные запросы
- [ ] CORS настроен правильно в backend
- [ ] localStorage пустой (или есть валидный токен)

---

## ?? Финальный тест

### 1. Останови всё
```powershell
Get-Process dotnet,prosushimsg -ErrorAction SilentlyContinue | Stop-Process -Force
```

### 2. Запусти Backend
```powershell
cd prosushimsg
dotnet run
```

**Жди:** `Now listening on: http://localhost:5000`

### 3. Запусти Frontend (новый терминал)
```powershell
cd ProSushiMsg.Client
dotnet watch run
```

**Жди:** `Now listening on: http://localhost:5001`

### 4. Открой браузер с DevTools
```
http://localhost:5001
```

**F12 ? Console ? смотри логи**

---

## ?? Если всё ещё не работает

Скопируй из Console (F12):

1. **Все логи с эмодзи** (`??`, `?`, etc.)
2. **Network** ? найди красный запрос ? Response Tab ? скопируй содержимое
3. **Stack Trace** если есть исключение

И покажи эти логи - я помогу разобраться точно где проблема!

---

**? После этих шагов ты точно найдёшь причину ошибки!** ??
