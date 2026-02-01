using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProSushiMsg.Data;
using ProSushiMsg.Models;

namespace ProSushiMsg.Controllers;

[Authorize] // Только авторизованные пользователи
[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MessagesController(AppDbContext db)
    {
        _db = db;
    }

    // Получить историю личного чата с конкретным пользователем
    [HttpGet("direct/{otherUserId}")]
    public async Task<IActionResult> GetDirectMessages(int otherUserId, [FromQuery] int limit = 50)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        // Сообщения между мной и otherUserId (в обе стороны)
        var messages = await _db.Messages
            .Where(m => 
                (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .OrderBy(m => m.SentAt) // Сначала старые
            .ToListAsync();

        return Ok(messages);
    }

    // Получить историю группового чата
    [HttpGet("group/{groupId}")]
    public async Task<IActionResult> GetGroupMessages(int groupId, [FromQuery] int limit = 50)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        // Проверка, что пользователь в этой группе (упрощённо, без связи многие-ко-многим)
        // В проде нужна таблица GroupMembers

        var messages = await _db.Messages
            .Where(m => m.GroupId == groupId)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return Ok(messages);
    }

    // Получить список всех диалогов (недавние чаты)
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        // Последние сообщения из каждого диалога
        var conversations = await _db.Messages
            .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
            .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
            .Select(g => new
            {
                UserId = g.Key,
                LastMessage = g.OrderByDescending(m => m.SentAt).FirstOrDefault(),
                UnreadCount = g.Count(m => m.ReceiverId == currentUserId && !m.IsRead)
            })
            .ToListAsync();

        return Ok(conversations);
    }

    // Пометить сообщения как прочитанные
    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkAsRead([FromBody] MarkReadRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var messages = await _db.Messages
            .Where(m => request.MessageIds.Contains(m.Id) && m.ReceiverId == currentUserId)
            .ToListAsync();

        foreach (var msg in messages)
        {
            msg.IsRead = true;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    // Удалить сообщение (только своё)
    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessage(int messageId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var message = await _db.Messages.FindAsync(messageId);
        if (message == null || message.SenderId != currentUserId)
            return NotFound();

        _db.Messages.Remove(message);
        await _db.SaveChangesAsync();

        return Ok();
    }

    // Вспомогательный метод — получить userId из JWT токена
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("userId");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}

public record MarkReadRequest(List<int> MessageIds);
