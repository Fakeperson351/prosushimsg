# ?? Интеграция Backend + Blazor WASM

## 1?? Backend Changes Required

### A. Добавь E2EE поддержку в User модель

```csharp
// Models/User.cs
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    
    // ?? E2EE поля
    public string? PublicKeyHex { get; set; }  // Публичный ключ пользователя
    public DateTime? PublicKeyUpdatedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
}
```

### B. Добавь endpoint для сохранения публичного ключа

```csharp
// Controllers/UsersController.cs
[HttpPost("me/public-key")]
[Authorize]
public async Task<IActionResult> UpdatePublicKey([FromBody] UpdatePublicKeyRequest request)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    var user = await _context.Users.FindAsync(userId);
    
    if (user == null)
        return NotFound();
    
    user.PublicKeyHex = request.PublicKeyHex;
    user.PublicKeyUpdatedAt = DateTime.UtcNow;
    
    await _context.SaveChangesAsync();
    return Ok();
}

public class UpdatePublicKeyRequest
{
    public string PublicKeyHex { get; set; } = string.Empty;
}
```

### C. Обнови ChatHub для зашифрованных сообщений

```csharp
// Hubs/ChatHub.cs
[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _context;

    // Зашифрованное сообщение 1-на-1
    public async Task SendEncryptedMessage(string encryptedMessage, string signature)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
            return;

        // Логируем зашифрованное сообщение (не видим содержимое)
        Console.WriteLine($"?? {user.Username} отправил зашифрованное сообщение");
        
        // Отправляем только авторизованному получателю
        // (в реальном приложении — перейди на 1-на-1 контекст)
        var message = new Message
        {
            SenderId = userId,
            Content = encryptedMessage,
            Timestamp = DateTime.UtcNow,
            IsEncrypted = true,
            SignatureHex = signature
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        // Отправляем зашифрованное сообщение всем в группе
        await Clients.All.SendAsync("ReceiveEncryptedMessage", 
            userId, 
            user.Username, 
            encryptedMessage, 
            signature);
    }

    // Зашифрованное сообщение в группу
    public async Task SendEncryptedGroupMessage(int groupId, string encryptedMessage, string signature)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var group = await _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);
        
        if (group == null || !group.Members.Any(m => m.UserId == userId))
            return;

        var message = new Message
        {
            SenderId = userId,
            GroupId = groupId,
            Content = encryptedMessage,
            Timestamp = DateTime.UtcNow,
            IsEncrypted = true,
            SignatureHex = signature
        };

        await _context.Messages.AddAsync(message);
        await _context.SaveChangesAsync();

        await Clients.Groups($"group-{groupId}")
            .SendAsync("ReceiveEncryptedGroupMessage", 
                userId, 
                encryptedMessage, 
                signature);
    }
}
```

### D. Обнови Message модель

```csharp
// Models/Message.cs
public class Message
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;
    
    public int? ReceiverId { get; set; }
    public User? Receiver { get; set; }
    
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
    
    public string Content { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? FileType { get; set; }
    
    // ?? E2EE поля
    public bool IsEncrypted { get; set; }
    public string? SignatureHex { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}
```

### E. Миграция БД

```bash
dotnet ef migrations add AddE2EESupport
dotnet ef database update
```

---

## 2?? Blazor Frontend Integration

### A. Обнови Chats.razor для E2EE

```razor
@page "/chats"
@using ProSushiMsg.Client.Services
@inject AuthService AuthService
@inject ChatService ChatService
@inject SignalRService SignalRService
@inject EncryptionService EncryptionService
@inject LocalStorageService LocalStorage
@inject NavigationManager Navigation
@implements IAsyncDisposable

@* ... существующий код ... *@

@code {
    private string? _secretKeyHex;
    private Dictionary<int, string> _userPublicKeys = [];

    protected override async Task OnInitializedAsync()
    {
        // Проверка аутентификации
        if (!AuthService.IsAuthenticated)
            Navigation.NavigateTo("/login");

        // Загрузка или создание пары ключей
        _secretKeyHex = await LocalStorage.GetAsync("secret_key_hex");
        
        if (string.IsNullOrEmpty(_secretKeyHex))
        {
            // Генерируем новую пару
            var (publicKey, secretKey) = EncryptionService.GenerateKeyPair();
            _secretKeyHex = secretKey;
            
            await LocalStorage.SetAsync("secret_key_hex", secretKey);
            
            // Отправляем публичный ключ на сервер
            await ChatService.UpdatePublicKeyAsync(publicKey);
        }
        else
        {
            // Загружаем сохранённый приватный ключ
            EncryptionService.LoadSecretKey(_secretKeyHex);
        }

        // Загрузка чатов и ключей
        Chats = await ChatService.GetChatsAsync();
        
        foreach (var chat in Chats)
        {
            var userPublicKey = await ChatService.GetUserPublicKeyAsync(chat.OtherUserId);
            if (userPublicKey != null)
                _userPublicKeys[chat.OtherUserId] = userPublicKey;
        }

        IsLoading = false;

        // Подключение к SignalR
        try
        {
            await SignalRService.ConnectAsync();
            SignalRService.OnMessageReceived += OnMessageReceived;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR error: {ex.Message}");
        }
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrEmpty(MessageInput) || SelectedChat == null)
            return;

        IsSending = true;
        try
        {
            // Получаем публичный ключ получателя
            if (!_userPublicKeys.ContainsKey(SelectedChat.OtherUserId))
            {
                var pubKey = await ChatService.GetUserPublicKeyAsync(SelectedChat.OtherUserId);
                if (pubKey != null)
                    _userPublicKeys[SelectedChat.OtherUserId] = pubKey;
            }

            // Шифруем сообщение
            var encrypted = EncryptionService.EncryptMessage(
                MessageInput, 
                _userPublicKeys[SelectedChat.OtherUserId]);

            var signature = EncryptionService.SignMessage(MessageInput);

            // Отправляем через SignalR
            await SignalRService.SendMessageAsync(encrypted, SelectedChat.Id);
            
            MessageInput = "";
            
            // Добавляем в локальный список (видим расшифрованное)
            Messages.Add(new MessageDto
            {
                SenderId = AuthService.CurrentUserId ?? 0,
                SenderName = "Ты",
                Content = MessageInput,
                Timestamp = DateTime.Now,
                IsRead = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Send error: {ex.Message}");
        }
        finally
        {
            IsSending = false;
        }
    }

    private void OnMessageReceived(int userId, string userName, string encryptedMessage)
    {
        try
        {
            // Расшифровываем сообщение
            var publicKey = _userPublicKeys.ContainsKey(userId) 
                ? _userPublicKeys[userId] 
                : null;

            if (publicKey == null)
            {
                Console.WriteLine("? Публичный ключ отправителя не найден");
                return;
            }

            var decrypted = EncryptionService.DecryptMessage(encryptedMessage, publicKey);

            Messages.Add(new MessageDto
            {
                SenderId = userId,
                SenderName = userName,
                Content = decrypted,
                Timestamp = DateTime.Now,
                IsRead = false
            });
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Ошибка расшифровки: {ex.Message}");
        }
    }
}
```

### B. Добавь методы в ChatService

```csharp
// Services/ChatService.cs
public async Task UpdatePublicKeyAsync(string publicKeyHex)
{
    try
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/users/me/public-key",
            new { publicKeyHex });
        
        if (!response.IsSuccessStatusCode)
            Console.WriteLine("Ошибка сохранения публичного ключа");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

public async Task<string?> GetUserPublicKeyAsync(int userId)
{
    try
    {
        var response = await _httpClient.GetAsync($"/api/users/{userId}/public-key");
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsAsync<UserPublicKeyResponse>();
            return result.PublicKeyHex;
        }
    }
    catch { }
    
    return null;
}

private class UserPublicKeyResponse
{
    public string PublicKeyHex { get; set; } = string.Empty;
}
```

---

## 3?? Запуск и Тестирование

### Backend Start

```bash
cd prosushimsg
dotnet run --configuration Development
# Запустится на http://localhost:5000
```

### Frontend Start

```bash
cd prosushimsg.client
dotnet watch run --project prosushimsg.client
# Запустится на http://localhost:5001 (Blazor WASM dev server)
```

### Тест в Postman

```
1. POST /api/auth/register
   {
     "username": "alice",
     "email": "alice@example.com",
     "password": "SecurePass123!"
   }

2. POST /api/auth/login
   {
     "username": "alice",
     "password": "SecurePass123!"
   }
   Response: { "token": "eyJ0eX...", "userId": 1 }

3. POST /api/users/me/public-key
   Headers: Authorization: Bearer {token}
   {
     "publicKeyHex": "ab12cd34..."
   }

4. WebSocket: ws://localhost:5000/chathub?access_token={token}
   Message: SendEncryptedMessage
   { "encryptedMessage": "...", "signature": "..." }
```

---

## ?? Безопасность Чек-лист

- [ ] HTTPS используется (обязательно!)
- [ ] CORS настроен правильно (не `AllowAnyOrigin` в продакшене)
- [ ] JWT expires проверяется
- [ ] Приватные ключи не логируются
- [ ] Nonce не переиспользуется (в libsodium это автоматически)
- [ ] Password hashing используется (bcrypt/PBKDF2)
- [ ] SignalR использует Secure WebSocket (WSS)

---

## ?? Готово!

Ты только что:
? Создал Blazor WASM + PWA клиент  
? Интегрировал E2EE шифрование  
? Подключил реал-тайм SignalR  
? Настроил JWT авторизацию  

**Дальше:**
- Интеграция с Yandex SpeechKit?
- Тестирование на мобильных?
- Деплой на VPS?

