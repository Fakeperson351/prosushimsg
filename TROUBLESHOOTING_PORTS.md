# ?? Скрипт для освобождения портов

## Windows (PowerShell)

### Найти процесс на порту
```powershell
# Backend (5000)
netstat -ano | findstr :5000

# Frontend (5001)
netstat -ano | findstr :5001

# Любой порт (замени XXXX)
netstat -ano | findstr :XXXX
```

### Убить процесс
```powershell
# Замени 12345 на PID из предыдущей команды
taskkill /PID 12345 /F
```

### Один скрипт для всех портов
```powershell
# kill-ports.ps1
$ports = @(5000, 5001, 59626)

foreach ($port in $ports) {
    Write-Host "Проверяем порт $port..." -ForegroundColor Yellow
    $process = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    
    if ($process) {
        $pid = $process.OwningProcess
        Write-Host "Найден процесс PID: $pid на порту $port" -ForegroundColor Red
        Stop-Process -Id $pid -Force
        Write-Host "Процесс $pid убит!" -ForegroundColor Green
    } else {
        Write-Host "Порт $port свободен" -ForegroundColor Green
    }
}
```

**Запуск:**
```powershell
.\kill-ports.ps1
```

---

## Linux / macOS

### Найти процесс
```bash
# Backend (5000)
lsof -i :5000

# Frontend (5001)
lsof -i :5001
```

### Убить процесс
```bash
# Замени 12345 на PID
kill -9 12345

# Или автоматически
lsof -ti :5000 | xargs kill -9
lsof -ti :5001 | xargs kill -9
```

---

## ?? Запуск после очистки портов

### Backend
```powershell
cd prosushimsg
dotnet run
```
**? Запустится на `http://localhost:5000`**

### Frontend
```powershell
cd ProSushiMsg.Client
dotnet watch run
```
**? Запустится на `http://localhost:5001`**

---

## ??? Альтернатива: Изменить порты

### Backend (appsettings.json)
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5002"
      }
    }
  }
}
```

### Frontend (launchSettings.json)
```json
{
  "profiles": {
    "ProSushiMsg.Client": {
      "applicationUrl": "http://localhost:5003"
    }
  }
}
```

---

## ?? Частые проблемы

### Порт 59626 занят
Это порт который Visual Studio или dotnet watch может использовать случайно.

**Решение:**
```powershell
# Закрой все dotnet процессы
Get-Process dotnet | Stop-Process -Force

# Или перезагрузи компьютер
```

### HTTPS ошибки
Если видишь ошибки с HTTPS, отключи:
```powershell
# В appsettings.Development.json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

---

## ?? Чек-лист

- [ ] Закрыты все dotnet процессы
- [ ] Порты 5000 и 5001 свободны
- [ ] Backend запущен на 5000
- [ ] Frontend запущен на 5001
- [ ] Браузер открыт на http://localhost:5001

---

**Готово! Теперь всё должно работать! ??**
