using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using ProSushiMsg.Data;
using ProSushiMsg.Hubs;
using ProSushiMsg.Services;

var builder = WebApplication.CreateBuilder(args);

// Настройка портов явно
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); // HTTP на всех интерфейсах (0.0.0.0:5000)
});

// База данных SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=prosushi.db"));

// JWT авторизация
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = JwtService.GetValidationParameters();
        
        // Для SignalR — токен приходит в query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR(); // SignalR для реал-тайм
builder.Services.AddSingleton<JwtService>();
builder.Services.AddControllers();

// CORS для клиентов (Blazor WASM)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5001", 
                "https://localhost:5001",
                "http://localhost:5296",  // Visual Studio может использовать динамические порты
                "https://localhost:5296"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Для SignalR
    });
});

var app = builder.Build();

// Создать базу при первом запуске
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Настройка для Blazor WebAssembly (клиент)
app.UseBlazorFrameworkFiles(); // Обслуживание файлов Blazor WASM
app.UseStaticFiles();          // Обслуживание статических файлов (CSS, JS, images)

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chathub"); // WebSocket endpoint
app.MapFallbackToFile("index.html"); // SPA fallback для клиента

app.Run();

