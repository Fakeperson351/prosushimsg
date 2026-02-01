using System.Net.Http.Json;
using System.Text.Json;

namespace ProSushiMsg.Client.Services;

/// <summary>
/// Сервис аутентификации. Управляет JWT токеном и состоянием пользователя.
/// </summary>
public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly LocalStorageService _localStorage;
    public event Action? OnAuthChanged;

    public string? CurrentToken { get; private set; }
    public int? CurrentUserId { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentToken);

    public AuthService(HttpClient httpClient, LocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        Console.WriteLine("? AuthService создан");
    }

    /// <summary>
    /// Загружает сохранённый токен при загрузке приложения.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            CurrentToken = await _localStorage.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(CurrentToken))
            {
                CurrentUserId = await _localStorage.GetAsync<int>("auth_user_id");
                
                // Добавляем токен в заголовки HTTP
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentToken);
                
                OnAuthChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка инициализации AuthService: {ex.Message}");
            CurrentToken = null;
            CurrentUserId = null;
        }
    }

    /// <summary>
    /// Регистрация нового пользователя.
    /// </summary>
    public async Task<(bool Success, string? Error)> RegisterAsync(string username, string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", new
            {
                username,
                email,
                password
            });

            if (response.IsSuccessStatusCode)
                return (true, null);

            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Вход в систему. Сохраняет JWT токен.
    /// </summary>
    public async Task<(bool Success, string? Error)> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", new { username, password });

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                
                // Опции для десериализации (case-insensitive)
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                };
                
                var result = JsonSerializer.Deserialize<LoginResponse>(json, options);
                
                if (result == null || string.IsNullOrEmpty(result.Token))
                    return (false, "Неверный формат ответа от сервера");

                CurrentToken = result.Token;
                CurrentUserId = result.UserId;

                await _localStorage.SetAsync("auth_token", CurrentToken);
                await _localStorage.SetAsync("auth_user_id", CurrentUserId);

                // Добавляем токен в заголовки HTTP
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CurrentToken);

                OnAuthChanged?.Invoke();
                return (true, null);
            }

            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Выход из системы. Удаляет токен.
    /// </summary>
    public async Task LogoutAsync()
    {
        CurrentToken = null;
        CurrentUserId = null;
        await _localStorage.RemoveAsync("auth_token");
        await _localStorage.RemoveAsync("auth_user_id");
        _httpClient.DefaultRequestHeaders.Authorization = null;
        OnAuthChanged?.Invoke();
    }

    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
