using Microsoft.AspNetCore.SignalR;
using ProSushiMsg.Models;
using ProSushiMsg.Data;
using Microsoft.EntityFrameworkCore;

namespace ProSushiMsg.Hubs;

// SignalR Hub — сердце реал-тайм мессенджера
public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    private static readonly Dictionary<string, int> _connections = new(); // ConnectionId -> UserId

    public ChatHub(AppDbContext db)
    {
        _db = db;
    }

    // Когда пользователь подключается
    public override async Task OnConnectedAsync()
    {
        // Получаем userId из JWT токена
        var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var id))
        {
            _connections[Context.ConnectionId] = id;
            
            // Обновляем статус "онлайн"
            var user = await _db.Users.FindAsync(id);
            if (user != null)
            {
                user.IsOnline = true;
                await _db.SaveChangesAsync();
            }
            
            // Уведомляем всех о статусе
            await Clients.All.SendAsync("UserStatusChanged", id, true);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connections.TryGetValue(Context.ConnectionId, out var userId))
        {
            _connections.Remove(Context.ConnectionId);
            
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsOnline = false;
                await _db.SaveChangesAsync();
            }
            
            await Clients.All.SendAsync("UserStatusChanged", userId, false);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    // Отправка личного сообщения
    public async Task SendMessage(int receiverId, string content, MessageType type = MessageType.Text)
    {
        if (!_connections.TryGetValue(Context.ConnectionId, out var senderId))
            return;

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            Type = type,
            SentAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        // Найти ConnectionId получателя
        var receiverConnection = _connections.FirstOrDefault(x => x.Value == receiverId).Key;
        if (receiverConnection != null)
        {
            await Clients.Client(receiverConnection).SendAsync("ReceiveMessage", message);
        }

        // Отправителю подтверждение
        await Clients.Caller.SendAsync("MessageSent", message.Id);
    }

    // Отправка в группу
    public async Task SendGroupMessage(int groupId, string content)
    {
        if (!_connections.TryGetValue(Context.ConnectionId, out var senderId))
            return;

        var message = new Message
        {
            SenderId = senderId,
            GroupId = groupId,
            Content = content,
            Type = MessageType.Text,
            SentAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        // Отправить всем в группе
        await Clients.Group(groupId.ToString()).SendAsync("ReceiveGroupMessage", message);
    }

    // Присоединиться к группе (для SignalR)
    public async Task JoinGroup(int groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId.ToString());
    }
}
