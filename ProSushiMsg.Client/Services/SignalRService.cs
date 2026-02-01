using Microsoft.AspNetCore.SignalR.Client;

namespace ProSushiMsg.Client.Services;

/// <summary>
/// Управляет SignalR подключением к ChatHub.
/// Обрабатывает реал-тайм сообщения и события онлайн/оффлайн.
/// </summary>
public class SignalRService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly AuthService _authService;
    private readonly string _hubUrl;

    public event Action<int, string, string>? OnMessageReceived;
    public event Action<int, bool>? OnUserStatusChanged;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public SignalRService(AuthService authService, string hubUrl = "http://localhost:5000")
    {
        _authService = authService;
        _hubUrl = hubUrl;
        Console.WriteLine($"? SignalRService создан с URL: {_hubUrl}");
    }

    /// <summary>
    /// Подключается к ChatHub с JWT токеном.
    /// </summary>
    public async Task ConnectAsync()
    {
        if (IsConnected)
            return;

        if (string.IsNullOrEmpty(_authService.CurrentToken))
            throw new InvalidOperationException("Требуется аутентификация");

        _connection = new HubConnectionBuilder()
            .WithUrl($"{_hubUrl}/chathub?access_token={_authService.CurrentToken}")
            .WithAutomaticReconnect(
                new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10) })
            .AddJsonProtocol() // Используем JSON вместо MessagePack
            .Build();

        // Обработчики сообщений
        _connection.On<int, string, string>("ReceiveMessage", (userId, userName, message) =>
        {
            OnMessageReceived?.Invoke(userId, userName, message);
        });

        _connection.On<int, bool>("UserStatusChanged", (userId, isOnline) =>
        {
            OnUserStatusChanged?.Invoke(userId, isOnline);
        });

        _connection.Reconnecting += error =>
        {
            Console.WriteLine($"Переподключение... {error?.Message}");
            return Task.CompletedTask;
        };

        _connection.Reconnected += async (connectionId) =>
        {
            Console.WriteLine($"Переподключился! Connection ID: {connectionId}");
            OnConnected?.Invoke();
            await Task.CompletedTask;
        };

        _connection.Closed += error =>
        {
            Console.WriteLine($"Разорвано: {error?.Message}");
            OnDisconnected?.Invoke();
            return Task.CompletedTask;
        };

        await _connection.StartAsync();
        OnConnected?.Invoke();
    }

    /// <summary>
    /// Отправляет сообщение через SignalR.
    /// </summary>
    public async Task SendMessageAsync(string message, int? groupId = null)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Не подключено к серверу");

        if (groupId.HasValue)
            await _connection!.SendAsync("SendGroupMessage", groupId.Value, message);
        else
            await _connection!.SendAsync("SendMessage", message);
    }

    /// <summary>
    /// Отправляет статус "печатаю".
    /// </summary>
    public async Task SendTypingAsync(int? groupId = null)
    {
        if (!IsConnected)
            return;

        await _connection!.SendAsync("UserTyping", groupId);
    }

    /// <summary>
    /// Отправляет событие о прочтении сообщения.
    /// </summary>
    public async Task MarkAsReadAsync(int messageId)
    {
        if (!IsConnected)
            return;

        await _connection!.SendAsync("MarkMessageAsRead", messageId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
