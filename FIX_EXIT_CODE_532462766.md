# ?? Полная диагностика Exit Code -532462766

## ? Что было исправлено (финальная версия):

### 1. SignalRService
? **Было:** Требовал `IConfiguration` который недоступен в Blazor WASM  
? **Исправлено:** Использует фабрику с передачей URL напрямую

### 2. Порядок создания сервисов
? Все сервисы теперь создаются в правильном порядке с логами

### 3. Логирование
? Каждый конструктор сервиса пишет лог при создании

---

## ?? Перезапуск с полной диагностикой:

```powershell
# Останови всё
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force

# Очисти кэш
cd ProSushiMsg.Client
dotnet clean

# Запусти
dotnet watch run
```

---

## ?? Что ты должен увидеть в Console (F12):

### ? Успешный запуск:
```
? Все сервисы зарегистрированы
? LocalStorageService создан
? EncryptionService создан (Sodium будет инициализирован при первом использовании)
? AuthService создан
? ChatService создан
? SignalRService создан с URL: http://localhost:5000
?? Index.razor: Начало инициализации
```

### ? Если ошибка при создании сервисов:
```
? Все сервисы зарегистрированы
? LocalStorageService создан
?? КРИТИЧЕСКАЯ ОШИБКА при запуске:
Message: Cannot instantiate implementation type ...
```

**? Покажи мне ВЕСЬ лог!**

---

## ?? Если ошибка Exit Code -532462766 всё ещё есть:

### Шаг 1: Проверь Terminal где запущен dotnet watch
Должно быть:
```
watch : Building...
watch : Started
```

Если видишь:
```
Process terminated with code -532462766
```

**? Скопируй ВСЕ логи из терминала!**

---

### Шаг 2: Откройdevtools (F12) ? Console

Посмотри **самый первый лог**. Должен быть один из:

#### A. Ошибка при загрузке Blazor:
```
crit: Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer[100]
      Unhandled exception rendering component: ...
```

#### B. Ошибка при создании сервиса:
```
?? КРИТИЧЕСКАЯ ОШИБКА при запуске:
Message: ...
```

#### C. Sodium.Core ошибка:
```
Error: Could not load file or assembly 'libsodium'
```

**? Скопируй Stack Trace полностью!**

---

### Шаг 3: Network Tab (F12 ? Network)

Перезагрузи страницу и посмотри:

1. **_framework/blazor.webassembly.js** ? должен быть 200 OK
2. **_framework/dotnet.wasm** ? должен быть 200 OK
3. **_framework/Sodium.Core.dll** ? должен быть 200 OK

**Если какой-то файл 404 или Failed:**
? Скопируй имя файла и статус!

---

## ??? Возможные решения:

### Проблема 1: Sodium.Core не загружается в WASM

**Решение:** Временно отключи EncryptionService
```csharp
// В Program.cs закомментируй:
// builder.Services.AddScoped<EncryptionService>();
```

Если после этого приложение запустится ? проблема в Sodium.Core для WASM.

---

### Проблема 2: IJSRuntime не готов

LocalStorageService требует IJSRuntime который может быть не готов.

**Решение:** Проверь что используется правильный IJSRuntime:
```csharp
builder.Services.AddScoped<LocalStorageService>();
```

Это должно работать, но если нет ? покажи лог.

---

### Проблема 3: Circular Dependency

Если видишь:
```
System.InvalidOperationException: A circular dependency was detected
```

**? Покажи весь Stack Trace!**

---

## ?? Финальный тест:

```powershell
# 1. Убей все процессы
Get-Process dotnet,prosushimsg -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. Очисти всё
cd ProSushiMsg.Client
Remove-Item -Recurse -Force bin, obj -ErrorAction SilentlyContinue
dotnet clean

# 3. Восстанови пакеты
dotnet restore

# 4. Собери
dotnet build

# 5. Если build успешен ? запусти
dotnet watch run
```

---

## ?? Чек-лист:

- [ ] `dotnet clean` выполнен
- [ ] `dotnet restore` успешен
- [ ] `dotnet build` без ошибок
- [ ] Backend запущен на порту 5000
- [ ] DevTools (F12) открыт
- [ ] Console показывает логи создания сервисов

---

## ?? Если ничего не помогло:

Скопируй и пришли:

1. **Весь вывод Terminal** (где dotnet watch run)
2. **Весь Console (F12)** - особенно первые 20 строк
3. **Network Tab** - скриншот красных запросов
4. **Версию .NET:**
```powershell
dotnet --version
```

---

**? С этими логами я точно найду проблему!** ??
