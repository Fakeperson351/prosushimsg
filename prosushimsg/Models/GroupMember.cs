namespace ProSushiMsg.Models;

// Связующая таблица для групп и пользователей (многие-ко-многим)
public class GroupMember
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsAdmin { get; set; } // Для будущего функционала

    // Навигационные свойства
    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
