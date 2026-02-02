# 🚀 Инструкция по деплою ProSushi Messenger

## 📋 Предварительные требования

- **VPS с Ubuntu/Debian** (512 МБ RAM минимум)
- **IP:** 176.119.159.187
- **Домен:** chat.moviequotebot.ru (настроен через Cloudflare)
- **.NET 8+ Runtime** установлен на сервере

---

## 🛠️ Шаг 1: Подготовка на локальной машине

### Windows (PowerShell):
```powershell
# Запусти скрипт деплоя
.\deploy.ps1

# Создастся папка publish/server со всеми файлами
# И архив prosushi-release.zip
```

### Linux/Mac (Bash):
```bash
chmod +x deploy.sh
./deploy.sh

# Архивируй вручную
tar -czf prosushi-release.tar.gz -C publish/server .
```

---

## 🌐 Шаг 2: Загрузка на сервер

### Через SCP:
```bash
# Загрузить архив
scp prosushi-release.zip root@176.119.159.187:/tmp/

# Загрузить systemd service
scp prosushimsg.service root@176.119.159.187:/tmp/
```

### Через SFTP (FileZilla, WinSCP):
1. Подключись к серверу
2. Загрузи `prosushi-release.zip` в `/tmp/`
3. Загрузи `prosushimsg.service` в `/tmp/`

---

## 🖥️ Шаг 3: Установка на сервере

Подключись к серверу по SSH (от root):
```bash
ssh root@176.119.159.187
```

### 3.1. Установка зависимостей
```bash
# Проверь, установлен ли .NET
dotnet --version

# Если нет — установи .NET 8 Runtime:
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

apt update
apt install -y aspnetcore-runtime-8.0

# Проверь версию
dotnet --version  # Должно быть 8.0+

# Проверь путь к dotnet (для systemd)
which dotnet
# Должно вывести: /usr/bin/dotnet
```

### 3.2. Создание папки приложения
```bash
# Создай папку для приложения
mkdir -p /var/www/prosushimsg
chown -R www-data:www-data /var/www/prosushimsg

# Распакуй архив
cd /var/www/prosushimsg
unzip -o /tmp/prosushi-release.zip

# Проверь структуру
ls -la
# Должны быть: prosushimsg.dll, wwwroot/, appsettings.json и т.д.

# ⚠️ ВАЖНО: Проверь, что есть prosushimsg.dll (не .exe!)
ls -l prosushimsg.dll
```

### 3.3. Настройка systemd сервиса
```bash
# Скопируй systemd unit
cp /tmp/prosushimsg.service /etc/systemd/system/

# Перезагрузи systemd
systemctl daemon-reload

# Включи автозапуск
systemctl enable prosushimsg

# Запусти сервис
systemctl start prosushimsg

# Проверь статус
systemctl status prosushimsg
```

### 3.4. Проверка логов
```bash
# Смотреть логи в реальном времени
journalctl -u prosushimsg -f

# Последние 100 строк
journalctl -u prosushimsg -n 100

# Если ошибка 203/EXEC:
journalctl -u prosushimsg -n 20 --no-pager
```

---

## 🌍 Шаг 4: Настройка Cloudflare Tunnel (для HTTPS)

### 4.1. Установка cloudflared
```bash
wget https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
sudo dpkg -i cloudflared-linux-amd64.deb
```

### 4.2. Авторизация и создание туннеля
```bash
# Авторизуйся в Cloudflare
cloudflared tunnel login

# Создай туннель
cloudflared tunnel create prosushi-msg

# Запомни Tunnel ID (например: 12345678-1234-1234-1234-123456789abc)
```

### 4.3. Настройка конфигурации
Создай файл `/root/.cloudflared/config.yml`:
```yaml
tunnel: 12345678-1234-1234-1234-123456789abc  # ЗАМЕНИ на свой ID!
credentials-file: /root/.cloudflared/12345678-1234-1234-1234-123456789abc.json

ingress:
  - hostname: chat.moviequotebot.ru
    service: http://localhost:5000
  - service: http_status:404
```

### 4.4. Настройка DNS
```bash
# Привязать домен к туннелю
cloudflared tunnel route dns prosushi-msg chat.moviequotebot.ru
```

### 4.5. Запуск туннеля как сервис
```bash
sudo cloudflared service install
sudo systemctl start cloudflared
sudo systemctl enable cloudflared

# Проверка
sudo systemctl status cloudflared
```

---

## ✅ Шаг 5: Проверка работы

### Локальная проверка на сервере:
```bash
curl http://localhost:5000/api/auth/test
# Должен вернуть ответ от API
```

### Проверка через интернет:
Открой в браузере: **https://chat.moviequotebot.ru**

Должна загрузиться страница авторизации! 🎉

---

## 🔄 Обновление приложения

### Быстрое обновление:
```bash
# На локальной машине
.\deploy.ps1

# Загрузи на сервер
scp prosushi-release.zip root@176.119.159.187:/tmp/

# На сервере (от root, без sudo)
systemctl stop prosushimsg
cd /var/www/prosushimsg
unzip -o /tmp/prosushi-release.zip
systemctl start prosushimsg
systemctl status prosushimsg
```

---

## 🐛 Troubleshooting (Решение проблем)

### Проблема: Ошибка 203/EXEC (systemd не может запустить)
```bash
# 1. Проверь, установлен ли .NET
dotnet --version

# 2. Проверь, есть ли файл prosushimsg.dll
ls -l /var/www/prosushimsg/prosushimsg.dll

# 3. Проверь путь к dotnet в service
cat /etc/systemd/system/prosushimsg.service | grep ExecStart

# 4. Попробуй запустить вручную
cd /var/www/prosushimsg
dotnet prosushimsg.dll
# Если работает — проблема в systemd

# 5. Если dotnet не найден — добавь полный путь в service:
which dotnet
# Замени в prosushimsg.service строку ExecStart на:
# ExecStart=/usr/bin/dotnet /var/www/prosushimsg/prosushimsg.dll

systemctl daemon-reload
systemctl restart prosushimsg
```

### Проблема: 502 Bad Gateway
```bash
# Проверь, запущен ли сервис
systemctl status prosushimsg

# Проверь логи
journalctl -u prosushimsg -n 50
```

### Проблема: SignalR не подключается (405 Error)
- ✅ Проверь CORS в `Program.cs` (должен быть твой домен)
- ✅ Проверь, что в URL нет двойного слэша
- ✅ Перезапусти сервис: `systemctl restart prosushimsg`

### Проблема: База данных не создаётся
```bash
# Проверь права доступа
ls -la /var/www/prosushimsg/prosushi.db

# Если файла нет — создай вручную
cd /var/www/prosushimsg
touch prosushi.db
chown www-data:www-data prosushi.db
chmod 644 prosushi.db
```

### Проблема: Мало памяти (Out of Memory)
```bash
# Проверь потребление памяти
systemctl status prosushimsg | grep Memory

# Если превышает лимит — уменьши в prosushimsg.service:
# MemoryLimit=300M

systemctl daemon-reload
systemctl restart prosushimsg
```

---

## 📊 Мониторинг

### Использование памяти:
```bash
free -h
ps aux | grep prosushimsg
```

### Размер базы данных:
```bash
ls -lh /var/www/prosushimsg/prosushi.db
```

### Количество подключений:
```bash
sudo netstat -tuln | grep 5000
```

---

## 🔐 Резервное копирование

### Ежедневный бэкап базы данных:
Создай файл `/etc/cron.daily/prosushi-backup`:
```bash
#!/bin/bash
cp /var/www/prosushimsg/prosushi.db /backups/prosushi_$(date +%Y%m%d).db
# Удаляем бэкапы старше 7 дней
find /backups -name "prosushi_*.db" -mtime +7 -delete
```

Сделай исполняемым:
```bash
sudo chmod +x /etc/cron.daily/prosushi-backup
sudo mkdir -p /backups
```

---

## 🎉 Готово!

Твой мессенджер работает на **https://chat.moviequotebot.ru** 🚀

**Полезные ссылки:**
- Логи: `journalctl -u prosushimsg -f`
- Перезапуск: `systemctl restart prosushimsg`
- Статус: `systemctl status prosushimsg`
- Мониторинг: `htop` (установи: `apt install htop`)
