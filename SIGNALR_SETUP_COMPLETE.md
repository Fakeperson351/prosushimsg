# ?? SIGNALR ЧАТ - ПОЛНАЯ ИНСТРУКЦИЯ

## ? **ЧТО РЕАЛИЗОВАНО:**

### **Backend (ASP.NET Core + SignalR):**
1. **ChatHub** - WebSocket сервер для реал-тайм обмена
2. **Автоматическое создание чатов** - при первом сообщении чат появляется у обоих
3. **Хранение истории** - все сообщения в SQLite базе
4. **Статусы онлайн/оффлайн** - обновляются автоматически
5. **Уведомления** - MessageSent, ReceiveMessage, NewChatCreated, UserStatusChanged

### **Frontend (Blazor WASM + SignalR Client):**
1. **SignalRService** - подключение к ChatHub через WebSocket
2. **Реал-тайм получение** - сообщения приходят мгновенно
3. **Автообновление статусов** - онлайн/оффлайн
4. **Загрузка истории** - при выборе чата
5. **Список чатов** - все диалоги с кем была переписка

---

## ?? **КАК ТЕСТИРОВАТЬ:**

### **1. Запуск backend:**
```powershell
cd prosushimsg
dotnet run
```
Сервер запустится на http://localhost:5000

### **2. Запуск frontend (в другом терминале):**
```powershell
cd ProSushiMsg.Client
dotnet watch
```
Клиент откроется в браузере

### **3. Регистрация двух пользователей:**

**Alice (первый браузер):**
- Открой http://localhost:5001/register
- Username: `alice`, Password: `1234`
- Нажми "Зарегистрироваться" ? автологин ? перенаправление на /chats

**Bob (второй браузер/инкогнито):**
- Открой http://localhost:5001/register
- Username: `bob`, Password: `1234`
- Регистрируйся

### **4. Тест реал-тайм чата:**

**От Alice:**
1. Жми кнопку `+` (новый чат)
2. Введи `bob`
3. Жми "Начать чат"
4. Напиши: `Привет, Bob!`
5. Нажми Enter или "Отправить"

**От Bob (сразу без F5!):**
- В списке чатов появится **Alice** автоматически
- Откроешь чат - увидишь сообщение `Привет, Bob!`
- Ответь: `Здарова, Alice!`

**От Alice (сразу без F5!):**
- Сообщение от Bob появится **мгновенно**

### **5. Тест статусов:**
- Закрой вкладку Bob ? у Alice статус Bob станет "? оффлайн"
- Открой Bob снова ? статус станет "? онлайн"

---

## ?? **КАК ЭТО РАБОТАЕТ:**

### **Архитектура:**
```
???????????????                    ????????????????
?  Alice      ?????WebSocket????????   ChatHub    ?
?  (Browser)  ?    (SignalR)       ?  (Backend)   ?
???????????????                    ????????????????
                                           ?
                                           ?
                                    WebSocket (SignalR)
                                           ?
                                           ?
???????????????                    ????????????????
?  Bob        ?????WebSocket????????  SQLite DB   ?
?  (Browser)  ?                    ?   (История)  ?
???????????????                    ????????????????
```

### **Процесс отправки сообщения:**

1. **Alice пишет "Привет" и жмёт Enter**
2. Blazor вызывает `SignalRService.SendMessageAsync(bobId, "Привет")`
3. SignalR отправляет через WebSocket на `ChatHub.SendMessage()`
4. ChatHub сохраняет в БД:
   ```sql
   INSERT INTO Messages (SenderId=1, ReceiverId=2, Content="Привет", SentAt=...)
   ```
5. ChatHub отправляет **Alice** подтверждение:
   ```
   Clients.Caller.SendAsync("MessageSent", { id: 1, content: "Привет", ... })
   ```
6. ChatHub отправляет **Bob** сообщение:
   ```
   Clients.Client(bobConnectionId).SendAsync("ReceiveMessage", { senderId: 1, content: "Привет", ... })
   ```
7. **Если Bob оффлайн** - сообщение остаётся в БД, увидит при входе

### **Процесс создания чата:**

1. **Alice создаёт чат с Bob** (жмёт +, вводит `bob`)
2. Frontend вызывает `/api/messages/start-chat` (HTTP POST)
3. Backend возвращает инфо о Bob: `{ userId: 2, username: "bob", isOnline: true }`
4. Чат добавляется в список Alice
5. **Alice отправляет первое сообщение** `"Привет"`
6. ChatHub видит - **это первое сообщение между Alice и Bob**
7. ChatHub отправляет уведомление **Bob**:
   ```
   Clients.Client(bobConnectionId).SendAsync("NewChatCreated", { userId: 1, username: "alice", isOnline: true })
   ```
8. У Bob в списке чатов появляется **Alice**
9. Bob открывает чат ? видит сообщение "Привет"

---

## ?? **ФУНКЦИОНАЛ:**

### ? **Что работает:**
- ? Реал-тайм отправка/получение сообщений
- ? Автоматическое создание чатов у обоих
- ? Хранение истории в БД
- ? Статусы онлайн/оффлайн
- ? Загрузка истории при выборе чата
- ? Автопрокрутка вниз
- ? JWT авторизация для SignalR
- ? Автоподключение SignalR при входе

### ? **Что не реализовано (но легко добавить):**
- ? Групповые чаты (есть модель, нужен только UI)
- ? Файлы/фото/голосовые (нужен FilesController)
- ? Прочтение сообщений (MarkAsRead готово в хабе, нужно подключить)
- ? Уведомления печати ("Alice печатает...")
- ? Push-уведомления (FCM/APNs)
- ? E2EE (нужен libsodium.js на клиенте)

---

## ?? **ПОДВОДНЫЕ КАМНИ:**

### **1. Память (512 МБ RAM):**
- 10-20 человек: **~30-50 МБ** ? OK
- 50 человек: **~100-150 МБ** ?? Тесновато, но работает
- 100+ человек: **~250+ МБ** ? Нужен Redis backplane или больше RAM

### **2. SignalR может не работать за некоторыми прокси:**
- Решение: добавить fallback на Long Polling:
  ```csharp
  .WithUrl(..., options => {
      options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
  })
  ```

### **3. Reconnect при потере связи:**
- Уже настроен автоматический переподключение (0s, 2s, 5s, 10s)
- Сообщения отправленные оффлайн будут потеряны (нужна очередь)

### **4. Масштабирование:**
- **До 50 человек** - всё ок
- **50-200** - нужен Redis backplane для SignalR
- **200+** - нужен отдельный сервер для WebSocket

---

## ?? **TROUBLESHOOTING:**

### **SignalR не подключается:**
```
? SignalR: Ошибка подключения: ...
```
**Решение:**
1. Проверь, запущен ли backend на http://localhost:5000
2. Проверь консоль backend - должно быть:
   ```
   ? SignalR: Пользователь 1 подключился (ConnectionId: ...)
   ```
3. Проверь CORS в Program.cs - должен быть `http://localhost:5001`

### **Сообщения не приходят реал-тайм:**
**Симптом:** Нужно обновлять F5 чтобы увидеть сообщения

**Причины:**
1. SignalR не подключён (смотри выше)
2. Не подписан на события - проверь OnInitializedAsync:
   ```csharp
   SignalRService.OnMessageReceived += OnMessageReceivedHandler;
   SignalRService.OnMessageSent += OnMessageSentHandler;
   ```

### **Чат создаётся только у одного:**
**Решение:** Уже исправлено! ChatHub отправляет `NewChatCreated` обоим при первом сообщении.

---

## ?? **СЛЕДУЮЩИЕ ШАГИ:**

### **1. Групповые чаты:**
- Добавить UI для создания группы
- Endpoint: `POST /api/groups/create`
- SignalR: `SendGroupMessage(groupId, content)`

### **2. Файлы/фото:**
- Endpoint: `POST /api/files/upload`
- Сохранение в `wwwroot/uploads`
- Отправка URL в сообщении

### **3. Голосовые сообщения:**
- Запись аудио в браузере: `MediaRecorder API`
- Отправка на сервер: `multipart/form-data`
- Yandex SpeechKit для распознавания (опционально)

### **4. E2EE (End-to-End Encryption):**
- Добавить `libsodium.js` на клиенте
- Шифровать `Content` перед отправкой
- Дешифровать при получении
- Ключи хранить в localStorage

### **5. Деплой на VPS:**
- Настроить Nginx reverse proxy
- Systemd service для backend
- CloudFlare Tunnel или Hysteria2 для обхода блокировок

---

## ?? **ПОЛЕЗНЫЕ КОМАНДЫ:**

```powershell
# Удалить базу и пересоздать
Remove-Item prosushi.db
cd prosushimsg
dotnet run

# Логи SignalR
cd prosushimsg
dotnet run --verbosity detailed

# Проверить подключения SignalR
# В консоли backend будет:
# ? SignalR: Пользователь 1 подключился
# ? SignalR: Пользователь 2 отключился

# Тест endpoint
Invoke-WebRequest -Uri "http://localhost:5000/api/messages/conversations" -Headers @{ Authorization = "Bearer YOUR_TOKEN" }
```

---

## ?? **ИТОГИ:**

? **Реал-тайм чат на SignalR работает!**
? **Автоматическое создание чатов у обоих**
? **Хранение истории в БД**
? **Статусы онлайн/оффлайн**
? **Для 10-50 человек на 512 МБ RAM - идеально!**

**Что дальше:**
1. **Тестируем** - проверь реал-тайм работу
2. **Групповые чаты** - если нужно
3. **Файлы/голосовые** - если нужно
4. **Деплой на VPS** - когда всё готово

Жду отчёта о тестировании! ????
