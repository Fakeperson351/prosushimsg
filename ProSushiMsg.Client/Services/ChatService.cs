using System.Net.Http.Json;
using System.Text.Json;

namespace ProSushiMsg.Client.Services;

/// <summary>
/// Сервис для работы с чатами, сообщениями и группами.
/// </summary>
public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ChatService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
        _jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };
        Console.WriteLine("? ChatService создан");
    }

    /// <summary>
    /// Получает список чатов текущего пользователя.
    /// </summary>
    public async Task<List<ChatDto>> GetChatsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/messages/chats");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ChatDto>>(json, _jsonOptions) ?? [];
            }
            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetChatsAsync error: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Получает сообщения конкретного чата.
    /// </summary>
    public async Task<List<MessageDto>> GetMessagesAsync(int chatId, int skip = 0, int take = 50)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/messages/{chatId}?skip={skip}&take={take}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<MessageDto>>(json, _jsonOptions) ?? [];
            }
            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetMessagesAsync error: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Получает список групп.
    /// </summary>
    public async Task<List<GroupDto>> GetGroupsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/groups");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<GroupDto>>(json, _jsonOptions) ?? [];
            }
            return [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetGroupsAsync error: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Создаёт новую группу.
    /// </summary>
    public async Task<(bool Success, int? GroupId)> CreateGroupAsync(string name, List<int> memberIds)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/groups", new
            {
                name,
                memberIds
            });

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<CreateGroupResponse>(json, _jsonOptions);
                return (true, result?.GroupId);
            }

            return (false, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CreateGroupAsync error: {ex.Message}");
            return (false, null);
        }
    }

    /// <summary>
    /// Загружает файл (фото, голосовое сообщение и т.д.).
    /// </summary>
    public async Task<(bool Success, string? FileUrl)> UploadFileAsync(
        byte[] fileData, 
        string fileName, 
        string contentType)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(fileData), "file", fileName);

            var response = await _httpClient.PostAsync("/api/files/upload", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<UploadResponse>(json, _jsonOptions);
                return (true, result?.FileUrl);
            }

            return (false, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UploadFileAsync error: {ex.Message}");
            return (false, null);
        }
    }

    private class CreateGroupResponse
    {
        public int GroupId { get; set; }
    }

    private class UploadResponse
    {
        public string FileUrl { get; set; } = string.Empty;
    }
}

// DTOs
public class ChatDto
{
    public int Id { get; set; }
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
    public bool IsOnline { get; set; }
}

public class MessageDto
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? FileType { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
    public int? GroupId { get; set; }
}

public class GroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
