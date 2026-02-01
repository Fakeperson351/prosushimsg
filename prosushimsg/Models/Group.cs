namespace ProSushiMsg.Models;

// Группа (чат для команды)
public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Связь многие-ко-многим
    public List<User> Members { get; set; } = new();
}
