# ?? Пошаговый запуск ProSushi Messenger

## ?? Перед запуском

### 1. Убедись что все процессы остановлены
```powershell
# Убей все dotnet процессы
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force

# Убей prosushimsg если запущен
taskkill /IM prosushimsg.exe /F 2>$null

# Или используй скрипт
.\kill-ports.ps1
```

### 2. Очисть build кэш (опционально)
```powershell
cd prosushimsg
dotnet clean

cd ..\ProSushiMsg.Client
dotnet clean
```

---

## ?? Запуск

### Terminal 1 — Backend (ASP.NET Core)

```powershell
cd prosushimsg
dotnet run
```

**Ожидаемый результат:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

? Backend запущен на **http://localhost:5000**

---

### Terminal 2 — Frontend (Blazor WASM)

Открой **НОВЫЙ терминал** и запусти:

```powershell
cd ProSushiMsg.Client
dotnet watch run
```

**Ожидаемый результат:**
```
watch : Hot reload enabled. For a list of supported edits
watch : Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5001
watch : Started
```

? Frontend запущен на **http://localhost:5001**

**Браузер автоматически откроется на http://localhost:5001**

---

## ?? Тестирование

### Шаг 1: Регистрация Alice

1. В браузере на `http://localhost:5001`
2. Нажми **"Зарегистрируйся"**
3. Заполни:
   - **Username:** `alice`
   - **Email:** `alice@example.com`
   - **Password:** `SecurePass123!`
4. Нажми **"Зарегистрироваться"**
5. Войди с теми же данными

### Шаг 2: Регистрация Bob (второй браузер)

1. Открой **инкогнито режим** (Ctrl+Shift+N в Chrome)
2. Перейди на `http://localhost:5001`
3. Зарегистрируй:
   - **Username:** `bob`
   - **Email:** `bob@example.com`
   - **Password:** `SecurePass123!`
4. Войди

### Шаг 3: Отправь сообщения

1. В браузере Alice напиши: `Привет, Bob!`
2. В браузере Bob **мгновенно** увидишь сообщение ?
3. Bob ответит: `Привет, Alice!`
4. Alice увидит ответ в реал-тайм! ??

**Поздравляю! Реал-тайм чат работает!** ??

---

## ? Если что-то не работает

### Backend не запускается

**Ошибка:** `Port 5000 already in use`

**Решение:**
```powershell
# Найди процесс
netstat -ano | findstr :5000

# Убей по PID (замени 12345)
taskkill /PID 12345 /F
```

---

### Frontend не подключается к SignalR

**Ошибка:** `Connection ID required`

**Причина:** Backend не запущен или неправильный URL

**Решение:**
1. Убедись что backend работает на `http://localhost:5000`
2. Проверь `ProSushiMsg.Client/wwwroot/appsettings.json`:
   ```json
   {
     "ApiBaseAddress": "http://localhost:5000"
   }
   ```

---

### Не видно сообщений в реал-тайм

**Причина:** SignalR не подключён

**Проверка в DevTools (F12):**
```
Console ? Должно быть:
[SignalR] Connected to http://localhost:5000/chathub
```

**Если нет:**
1. Проверь что backend запущен
2. Проверь токен: `localStorage.getItem('auth_token')`
3. Перелогинься

---

### SQLite ошибки

**Ошибка:** `SQLite Error: table User not found`

**Решение:**
```powershell
cd prosushimsg

# Удали базу
Remove-Item prosushi.db

# Пересоздай
dotnet ef database update

# Или просто запусти заново (EnsureCreated)
dotnet run
```

---

## ?? Проверка статуса

### Backend
```powershell
# Проверь что слушает на 5000
netstat -ano | findstr :5000

# Должно быть:
# TCP  127.0.0.1:5000  LISTENING
```

### Frontend
```powershell
# Проверь что слушает на 5001
netstat -ano | findstr :5001
```

### SignalR
Открой в браузере:
```
http://localhost:5000/chathub
```

**Ожидаемый ответ:**
```
Status Code: 400 (это нормально — SignalR требует WebSocket)
```

---

## ?? Чек-лист

- [ ] Backend запущен на порту 5000
- [ ] Frontend запущен на порту 5001
- [ ] Создал пользователя alice
- [ ] Создал пользователя bob (инкогнито)
- [ ] Отправил сообщение от alice
- [ ] Bob получил сообщение мгновенно
- [ ] Статусы онлайн/оффлайн работают

---

## ?? Быстрый перезапуск

```powershell
# 1. Останови всё
Get-Process dotnet,prosushimsg -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. Запусти backend
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd prosushimsg; dotnet run"

# 3. Запусти frontend (подожди 3 сек)
Start-Sleep 3
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd ProSushiMsg.Client; dotnet watch run"
```

---

## ?? Поддержка

Если ничего не помогло:

1. Проверь логи backend в терминале
2. Проверь Console в DevTools (F12)
3. Проверь Network ? WS вкладку (для SignalR)
4. Посмотри `TROUBLESHOOTING_PORTS.md`

---

**Готово! Теперь у тебя работает корпоративный мессенджер! ??**
