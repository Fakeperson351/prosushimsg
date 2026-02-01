namespace ProSushiMsg.Models;

// Модель пользователя для SQLite
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // Хранить только хеш!
    public string? FullName { get; set; }
    public string Role { get; set; } = "User"; // User, Courier, Manager
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; }
}
