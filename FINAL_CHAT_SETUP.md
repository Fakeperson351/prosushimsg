# ?? ФИНАЛЬНАЯ ИНСТРУКЦИЯ - Доделать работающий чат

## ? ЧТО УЖЕ РАБОТАЕТ:
- Регистрация и вход
- Создание чатов с пользователями
- JWT авторизация

## ?? ЧТО НУЖНО ДОДЕЛАТЬ (3 шага):

---

## **ШАГ 1: Backend - Добавить endpoint отправки сообщений**

### В `prosushimsg/Controllers/MessagesController.cs`:

Добавь перед методом `GetCurrentUserId()`:

```csharp
// Отправить личное сообщение
[HttpPost("send")]
public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
{
    var currentUserId = GetCurrentUserId();
    if (currentUserId == null) return Unauthorized();

    Console.WriteLine($"Отправка сообщения от {currentUserId} к {request.ReceiverId}");

    var sender = await _db.Users.FindAsync(currentUserId);
    var receiver = await _db.Users.FindAsync(request.ReceiverId);
    
    if (receiver == null)
        return NotFound(new { error = "Получатель не найден" });

    var message = new Message
    {
        SenderId = currentUserId.Value,
        ReceiverId = request.ReceiverId,
        Content = request.Content,
        SentAt = DateTime.UtcNow,
        IsRead = false
    };

    _db.Messages.Add(message);
    await _db.SaveChangesAsync();

    return Ok(new
    {
        id = message.Id,
        senderId = message.SenderId,
        senderName = sender?.Username ?? "Unknown",
        receiverId = message.ReceiverId,
        content = message.Content,
        timestamp = message.SentAt,
        isRead = message.IsRead
    });
}
```

### В конец файла (после `public record StartChatRequest...`):

```csharp
public record SendMessageRequest(int ReceiverId, string Content);
```

---

## **ШАГ 2: Frontend - Реализовать SendMessage в Chats.razor**

### Найди метод `SendMessage()` в Chats.razor и замени на:

```csharp
private async Task SendMessage()
{
    if (string.IsNullOrWhiteSpace(MessageInput) || SelectedChat == null || IsSending)
        return;

    try
    {
        IsSending = true;
        var content = MessageInput;
        MessageInput = ""; // Очищаем поле сразу
        StateHasChanged();

        Console.WriteLine($"Отправка сообщения к {SelectedChat.OtherUserId}: {content}");

        var httpClient = ChatService.GetHttpClient();
        
        // Добавляем токен
        if (!string.IsNullOrEmpty(AuthService.CurrentToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthService.CurrentToken);
        }

        var response = await httpClient.PostAsJsonAsync("/api/messages/send", new
        {
            receiverId = SelectedChat.OtherUserId,
            content = content
        });

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);

            var messageId = result.GetProperty("id").GetInt32();
            var senderName = result.GetProperty("senderName").GetString() ?? "";
            var timestamp = result.GetProperty("timestamp").GetDateTime();

            // Добавляем сообщение в список
            Messages.Add(new MessageDto
            {
                Id = messageId,
                SenderId = AuthService.CurrentUserId ?? 0,
                SenderName = senderName,
                Content = content,
                Timestamp = timestamp,
                IsRead = false
            });

            Console.WriteLine($"Сообщение отправлено успешно! ID: {messageId}");
            StateHasChanged();
        }
        else
        {
            Console.WriteLine($"Ошибка отправки: {response.StatusCode}");
            MessageInput = content; // Возвращаем текст обратно
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception: {ex.Message}");
    }
    finally
    {
        IsSending = false;
        StateHasChanged();
    }
}
```

---

## **ШАГ 3: Загрузка сообщений при выборе чата**

### Найди метод `SelectChat()` в Chats.razor и замени на:

```csharp
private async Task SelectChat(ChatDto chat)
{
    SelectedChat = chat;
    SelectedChatId = chat.Id;
    Messages.Clear();
    
    Console.WriteLine($"Загрузка сообщений для чата с {chat.OtherUserName} (ID: {chat.OtherUserId})");

    try
    {
        var httpClient = ChatService.GetHttpClient();
        
        // Добавляем токен
        if (!string.IsNullOrEmpty(AuthService.CurrentToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthService.CurrentToken);
        }

        var response = await httpClient.GetAsync($"/api/messages/direct/{chat.OtherUserId}");
        
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var messages = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);

            foreach (var msg in messages.EnumerateArray())
            {
                Messages.Add(new MessageDto
                {
                    Id = msg.GetProperty("id").GetInt32(),
                    SenderId = msg.GetProperty("senderId").GetInt32(),
                    SenderName = msg.GetProperty("senderId").GetInt32() == AuthService.CurrentUserId ? "Вы" : chat.OtherUserName,
                    Content = msg.GetProperty("content").GetString() ?? "",
                    Timestamp = msg.GetProperty("sentAt").GetDateTime(),
                    IsRead = msg.GetProperty("isRead").GetBoolean()
                });
            }

            Console.WriteLine($"Загружено {Messages.Count} сообщений");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка загрузки сообщений: {ex.Message}");
    }

    StateHasChanged();
}
```

---

## **ШАГ 4: Исправить UI кодировку (опционально)**

### В Chats.razor замени эмодзи на текст:

```razor
<!-- БЫЛО: -->
<h2>?? Чаты</h2>
<button>?</button>

<!-- СТАЛО: -->
<h2>Чаты</h2>
<button>+</button>

<!-- БЫЛО: -->
<button>??</button>
<button>??</button>

<!-- СТАЛО: -->
<button>Отправить</button>
<button>Файл</button>
```

Или добавь в `<head>` в `wwwroot/index.html`:

```html
<meta charset="utf-8" />
```

---

## ?? **ТЕСТ ПОСЛЕ ИЗМЕНЕНИЙ:**

1. **Перезапусти Backend:**
```powershell
cd prosushimsg
dotnet run
```

2. **Frontend автоматически пересоберётся** (dotnet watch)

3. **Тест:**
   - Зарегистрируй `alice` и `bob`
   - От alice создай чат с bob (кнопка +)
   - **Напиши сообщение и отправь**
   - В Console (F12) увидишь: `Сообщение отправлено успешно! ID: 1`
   - **От bob (в другом браузере/инкогнито) открой чат с alice**
   - Bob увидит сообщение от alice!

---

## ?? **Что будет работать:**

? Создание чатов  
? Отправка сообщений  
? Загрузка истории сообщений  
? Просмотр чатов  
? Авторизация  

---

## ?? **Что НЕ будет работать без SignalR:**

? Реал-тайм получение (нужно обновлять вручную F5)

Для реал-тайм нужно:
1. Подключить SignalR в OnInitializedAsync
2. Подписаться на OnMessageReceived
3. Добавлять сообщения в Messages при получении

Но это уже потом! Сначала проверь что отправка работает!

---

**?? Сделай эти 4 шага и чат заработает! ??**
