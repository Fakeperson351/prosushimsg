# 🚀 Скрипт деплоя ProSushi Messenger на VPS (Windows)

Write-Host "🔨 Сборка проекта..." -ForegroundColor Cyan

# Проверяем, что мы в корне проекта
if (-not (Test-Path "prosushimsg")) {
    Write-Host "❌ Ошибка: запусти скрипт из корня репозитория!" -ForegroundColor Red
    exit 1
}

# Удаляем старые файлы
if (Test-Path "publish") {
    Write-Host "🗑️ Удаление старой сборки..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force "publish"
}

# Создаём папки
New-Item -ItemType Directory -Force -Path "publish/server" | Out-Null
New-Item -ItemType Directory -Force -Path "publish/client" | Out-Null

Write-Host "📦 Сборка сервера (ASP.NET Core)..." -ForegroundColor Green
Set-Location "prosushimsg"
dotnet publish -c Release -o "../publish/server" --self-contained false
Set-Location ".."

Write-Host "📦 Сборка клиента (Blazor WASM)..." -ForegroundColor Green
Set-Location "ProSushiMsg.Client"
dotnet publish -c Release -o "../publish/client" --self-contained false
Set-Location ".."

Write-Host "📋 Копирование клиента в wwwroot сервера..." -ForegroundColor Cyan

# Удаляем старый wwwroot, если есть
if (Test-Path "publish/server/wwwroot") {
    Remove-Item -Recurse -Force "publish/server/wwwroot"
}

# Создаём новый wwwroot
New-Item -ItemType Directory -Force -Path "publish/server/wwwroot" | Out-Null

# Копируем ВСЁ содержимое wwwroot клиента (включая _framework!)
Copy-Item -Path "publish/client/wwwroot/*" -Destination "publish/server/wwwroot/" -Recurse -Force

Write-Host "✅ Клиент скопирован в wwwroot" -ForegroundColor Green

# Проверяем, что _framework есть
if (Test-Path "publish/server/wwwroot/_framework") {
    Write-Host "✅ _framework папка найдена (Blazor WASM работает)" -ForegroundColor Green
} else {
    Write-Host "⚠️  ВНИМАНИЕ: _framework папка НЕ найдена!" -ForegroundColor Red
    Write-Host "   Blazor не будет работать!" -ForegroundColor Red
}

Write-Host "`n✅ Сборка завершена!" -ForegroundColor Green
Write-Host "📦 Файлы готовы к деплою: ./publish/server" -ForegroundColor Cyan

Write-Host "`n📤 Следующие шаги:" -ForegroundColor Yellow
Write-Host "1️⃣  Архивируй файлы:" -ForegroundColor White
Write-Host "   Compress-Archive -Path publish/server/* -DestinationPath prosushi-release.zip" -ForegroundColor Gray
Write-Host ""
Write-Host "2️⃣  Загрузи на сервер через SCP или FTP" -ForegroundColor White
Write-Host "   scp prosushi-release.zip user@176.119.159.187:/tmp/" -ForegroundColor Gray
Write-Host ""
Write-Host "3️⃣  На сервере выполни:" -ForegroundColor White
Write-Host "   cd /var/www/prosushimsg" -ForegroundColor Gray
Write-Host "    systemctl stop prosushimsg" -ForegroundColor Gray
Write-Host "    unzip -o /tmp/prosushi-release.zip" -ForegroundColor Gray
Write-Host "    systemctl start prosushimsg" -ForegroundColor Gray
Write-Host ""
Write-Host "4️⃣  Проверь логи:" -ForegroundColor White
Write-Host "    journalctl -u prosushimsg -f" -ForegroundColor Gray
Write-Host ""

# Предложение автоматического архивирования
$compress = Read-Host "Создать архив prosushi-release.zip? (y/n)"
if ($compress -eq "y") {
    Write-Host "`n📦 Создание архива..." -ForegroundColor Cyan
    Compress-Archive -Path "publish/server/*" -DestinationPath "prosushi-release.zip" -Force
    Write-Host "✅ Архив создан: prosushi-release.zip ($([math]::Round((Get-Item prosushi-release.zip).Length / 1MB, 2)) MB)" -ForegroundColor Green
}

Write-Host "`n🎉 Готово!" -ForegroundColor Green
