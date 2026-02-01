using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ProSushiMsg.Client;
using ProSushiMsg.Client.Services;

try
{
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");

    // Настройка HttpClient с базовым адресом API
    var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? "http://localhost:5000";
    
    // Правильная регистрация HttpClient для всех сервисов
    builder.Services.AddScoped(sp => 
    {
        var client = new HttpClient { BaseAddress = new Uri(apiBaseAddress) };
        return client;
    });

    // Регистрация сервисов в правильном порядке (без circular dependencies)
    builder.Services.AddScoped<LocalStorageService>();
    builder.Services.AddScoped<EncryptionService>();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<ChatService>();
    
    // SignalRService с правильной фабрикой (получаем apiBaseAddress из конфигурации)
    builder.Services.AddScoped<SignalRService>(sp =>
    {
        var authService = sp.GetRequiredService<AuthService>();
        return new SignalRService(authService, apiBaseAddress);
    });

    Console.WriteLine("? Все сервисы зарегистрированы");

    
    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"?? КРИТИЧЕСКАЯ ОШИБКА при запуске:");
    Console.Error.WriteLine($"Message: {ex.Message}");
    Console.Error.WriteLine($"Type: {ex.GetType().Name}");
    Console.Error.WriteLine($"StackTrace: {ex.StackTrace}");
    
    if (ex.InnerException != null)
    {
        Console.Error.WriteLine($"InnerException: {ex.InnerException.Message}");
        Console.Error.WriteLine($"InnerStackTrace: {ex.InnerException.StackTrace}");
    }
    
    throw; // Пробрасываем дальше для отображения в браузере
}


