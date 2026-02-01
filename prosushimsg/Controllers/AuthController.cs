using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProSushiMsg.Data;
using ProSushiMsg.Models;
using ProSushiMsg.Services;
using System.Security.Cryptography;
using System.Text;

namespace ProSushiMsg.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwtService;

    public AuthController(AppDbContext db, JwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    // Регистрация
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        Console.WriteLine($"?? Register attempt: username='{request.Username}', email='{request.Email}', password='{request.Password}'");
        
        // Проверка, что username свободен
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
        {
            Console.WriteLine($"? Username already exists: {request.Username}");
            return BadRequest("Пользователь уже существует");
        }

        var passwordHash = HashPassword(request.Password);
        Console.WriteLine($"?? Generated password hash: {passwordHash}");

        var user = new User
        {
            Username = request.Username,
            PasswordHash = passwordHash,
            FullName = request.Email ?? request.Username, // Email используется как FullName
            Role = "User"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        Console.WriteLine($"? User registered successfully: {request.Username} (ID: {user.Id})");

        var token = _jwtService.GenerateToken(user.Id, user.Username, user.Role);
        return Ok(new { token, userId = user.Id, username = user.Username });
    }

    // Вход
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        Console.WriteLine($"?? Login attempt: username='{request.Username}', password='{request.Password}'");
        
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null)
        {
            Console.WriteLine($"? User not found: {request.Username}");
            return Unauthorized("Неверный логин или пароль");
        }

        var inputHash = HashPassword(request.Password);
        Console.WriteLine($"?? Password hash comparison:");
        Console.WriteLine($"   Input:  {inputHash}");
        Console.WriteLine($"   Stored: {user.PasswordHash}");
        Console.WriteLine($"   Match:  {inputHash == user.PasswordHash}");

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            Console.WriteLine($"? Password mismatch for user: {request.Username}");
            return Unauthorized("Неверный логин или пароль");
        }

        Console.WriteLine($"? Login successful: {request.Username}");
        var token = _jwtService.GenerateToken(user.Id, user.Username, user.Role);
        return Ok(new { token, userId = user.Id, username = user.Username, role = user.Role });
    }

    // Хеширование пароля (SHA256 — для прода лучше BCrypt!)
    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}

public record RegisterRequest(string Username, string Password, string? Email);
public record LoginRequest(string Username, string Password);
