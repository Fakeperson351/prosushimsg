# PowerShell скрипт для освобождения портов
# Использование: .\kill-ports.ps1

Write-Host "?? Очистка портов для ProSushi Messenger..." -ForegroundColor Cyan

$ports = @(5000, 5001, 59626)

foreach ($port in $ports) {
    Write-Host "`nПроверяем порт $port..." -ForegroundColor Yellow
    
    try {
        $connections = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
        
        if ($connections) {
            foreach ($conn in $connections) {
                $pid = $conn.OwningProcess
                $processName = (Get-Process -Id $pid).ProcessName
                
                Write-Host "  Найден процесс: $processName (PID: $pid)" -ForegroundColor Red
                
                # Убиваем процесс
                Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
                Write-Host "  ? Процесс $pid убит!" -ForegroundColor Green
            }
        } else {
            Write-Host "  ? Порт $port свободен" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "  ?? Не удалось проверить порт $port" -ForegroundColor Yellow
    }
}

Write-Host "`n?? Готово! Все порты очищены!" -ForegroundColor Cyan
Write-Host "`nТеперь запускай:" -ForegroundColor White
Write-Host "  cd prosushimsg && dotnet run" -ForegroundColor Gray
Write-Host "  cd ProSushiMsg.Client && dotnet watch run" -ForegroundColor Gray
