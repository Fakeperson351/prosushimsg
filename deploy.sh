#!/bin/bash
# 🚀 Скрипт деплоя ProSushi Messenger на VPS

set -e  # Остановить при ошибке

echo "🔨 Сборка проекта..."

# Переходим в папку сервера
cd prosushimsg

# Публикуем сервер (Release)
dotnet publish -c Release -o ../publish/server --self-contained false

# Переходим обратно
cd ..

# Публикуем клиент (Blazor WASM)
cd ProSushiMsg.Client
dotnet publish -c Release -o ../publish/client --self-contained false

cd ..

echo "✅ Сборка завершена!"
echo "📦 Файлы сервера: ./publish/server"
echo "📦 Файлы клиента: ./publish/client"

# Копируем клиент в папку wwwroot сервера
echo "📋 Копирование клиента в wwwroot сервера..."
cp -r ./publish/client/wwwroot/* ./publish/server/wwwroot/

echo "🎉 Готово к деплою!"
echo ""
echo "📤 Загрузи на сервер:"
echo "   scp -r ./publish/server/* user@176.119.159.187:/var/www/prosushimsg/"
echo ""
echo "🔄 На сервере выполни:"
echo "   sudo systemctl restart prosushimsg"
