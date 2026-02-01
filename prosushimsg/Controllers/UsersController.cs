using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProSushiMsg.Data;

namespace ProSushiMsg.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    // Получить список всех пользователей (для поиска при создании чата)
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] string? search = null)
    {
        var query = _db.Users.AsQueryable();

        // Поиск по имени/логину
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => 
                u.Username.Contains(search) || 
                (u.FullName != null && u.FullName.Contains(search)));
        }

        var users = await query
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.FullName,
                u.Role,
                u.IsOnline
            })
            .ToListAsync();

        return Ok(users);
    }

    // Получить информацию о конкретном пользователе
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.FullName,
            user.Role,
            user.IsOnline,
            user.CreatedAt
        });
    }

    // Получить список онлайн пользователей
    [HttpGet("online")]
    public async Task<IActionResult> GetOnlineUsers()
    {
        var users = await _db.Users
            .Where(u => u.IsOnline)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.FullName,
                u.Role
            })
            .ToListAsync();

        return Ok(users);
    }

    // Обновить профиль текущего пользователя
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!string.IsNullOrEmpty(request.FullName))
            user.FullName = request.FullName;

        await _db.SaveChangesAsync();
        return Ok(user);
    }

    // Получить текущего пользователя (из JWT)
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.FullName,
            user.Role,
            user.IsOnline,
            user.CreatedAt
        });
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("userId");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}

public record UpdateProfileRequest(string? FullName);
