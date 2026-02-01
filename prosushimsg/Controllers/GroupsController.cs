using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProSushiMsg.Data;
using ProSushiMsg.Models;

namespace ProSushiMsg.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GroupsController : ControllerBase
{
    private readonly AppDbContext _db;

    public GroupsController(AppDbContext db)
    {
        _db = db;
    }

    // Получить список всех групп пользователя
    [HttpGet]
    public async Task<IActionResult> GetMyGroups()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        // Группы, где пользователь участник
        var groups = await _db.GroupMembers
            .Where(gm => gm.UserId == userId)
            .Include(gm => gm.Group)
            .Select(gm => gm.Group)
            .ToListAsync();

        return Ok(groups);
    }

    // Создать новую группу
    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var group = new Group
        {
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        // Добавить создателя как администратора
        var member = new GroupMember
        {
            GroupId = group.Id,
            UserId = userId.Value,
            IsAdmin = true
        };
        _db.GroupMembers.Add(member);
        await _db.SaveChangesAsync();

        return Ok(group);
    }

    // Добавить участника в группу
    [HttpPost("{groupId}/members")]
    public async Task<IActionResult> AddMember(int groupId, [FromBody] AddMemberRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return NotFound();

        // Проверка, что текущий пользователь — админ группы
        var isAdmin = await _db.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == currentUserId && gm.IsAdmin);
        if (!isAdmin) return Forbid("Только админы могут добавлять участников");

        var user = await _db.Users.FindAsync(request.UserId);
        if (user == null) return NotFound("Пользователь не найден");

        // Проверка, что пользователь ещё не в группе
        var exists = await _db.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == request.UserId);
        if (exists) return BadRequest("Пользователь уже в группе");

        var member = new GroupMember
        {
            GroupId = groupId,
            UserId = request.UserId
        };
        _db.GroupMembers.Add(member);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Участник добавлен", groupId, userId = request.UserId });
    }

    // Покинуть группу
    [HttpDelete("{groupId}/leave")]
    public async Task<IActionResult> LeaveGroup(int groupId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var member = await _db.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        
        if (member == null) return NotFound("Вы не в этой группе");

        _db.GroupMembers.Remove(member);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Вы покинули группу", groupId });
    }

    // Получить участников группы
    [HttpGet("{groupId}/members")]
    public async Task<IActionResult> GetMembers(int groupId)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) return NotFound();

        var members = await _db.GroupMembers
            .Where(gm => gm.GroupId == groupId)
            .Include(gm => gm.User)
            .Select(gm => new
            {
                gm.User.Id,
                gm.User.Username,
                gm.User.FullName,
                gm.User.IsOnline,
                gm.IsAdmin,
                gm.JoinedAt
            })
            .ToListAsync();

        return Ok(members);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst("userId");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}

public record CreateGroupRequest(string Name);
public record AddMemberRequest(int UserId);
