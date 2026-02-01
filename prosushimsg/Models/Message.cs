namespace ProSushiMsg.Models;

// Сообщение
public class Message
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int? ReceiverId { get; set; } // null для групповых
    public int? GroupId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public string? FilePath { get; set; } // Для голосовых/фото
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}

public enum MessageType
{
    Text,
    Voice,
    Photo,
    File
}
