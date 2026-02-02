# ?? ДЕПЛОЙ PROSUSHIMSG НА VPS (Ubuntu/Debian)

## ?? **ЧТО НУЖНО:**

### **1. VPS Сервер:**
- **OS:** Ubuntu 22.04/24.04 или Debian 12
- **RAM:** Минимум 1 ГБ (512 МБ будет тесно с Nginx)
- **Диск:** 10-20 ГБ
- **CPU:** 1 ядро (достаточно)

### **2. Домен (опционально):**
- Купить на Cloudflare/Namecheap/REG.RU
- Настроить A-запись на IP VPS

### **3. Провайдеры VPS (рекомендации):**

#### **Российские (обход блокировок не нужен):**
- **Timeweb** - от 199?/мес, хорошая поддержка
- **REG.RU** - от 250?/мес
- **Selectel** - от 299?/мес

#### **Зарубежные (нужен обход блокировок):**
- **Hetzner** (Германия) - от €4/мес (~400?)
- **DigitalOcean** (США) - от $6/мес (~600?)
- **Vultr** (США/Европа) - от $6/мес

#### **Дешёвые VDS (для тестов):**
- **Contabo** - от €4/мес, но медленные диски
- **OVH** - от €3.50/мес

---

## ??? **УСТАНОВКА (ПОШАГОВО)**

### **ШАГ 1: ПОДКЛЮЧЕНИЕ К VPS**

```bash
# Windows (PowerShell)
ssh root@YOUR_VPS_IP

# Linux/Mac
ssh root@YOUR_VPS_IP

# При первом подключении согласись: yes
```

---

### **ШАГ 2: УСТАНОВКА .NET 10 SDK**

```bash
# Обновляем систему
sudo apt update && sudo apt upgrade -y

# Устанавливаем зависимости
sudo apt install -y wget apt-transport-https

# Добавляем репозиторий Microsoft
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Устанавливаем .NET 10 SDK
sudo apt update
sudo apt install -y dotnet-sdk-10.0

# Проверяем установку
dotnet --version
# Должно быть: 10.0.xxx
```

---

### **ШАГ 3: УСТАНАВЛИВАЕМ NGINX**

```bash
sudo apt install -y nginx

# Проверяем запуск
sudo systemctl status nginx

# Должно быть: active (running)

# Разрешаем в firewall
sudo ufw allow 'Nginx Full'
sudo ufw allow OpenSSH
sudo ufw enable
```

---

### **ШАГ 4: ЗАГРУЖАЕМ ПРОЕКТ НА VPS**

#### **Вариант А: Через Git (рекомендуется):**

```bash
# Устанавливаем Git
sudo apt install -y git

# Создаём папку для проекта
cd /var/www
sudo git clone https://github.com/YOUR_USERNAME/prosushimsg.git
cd prosushimsg

# Даём права
sudo chown -R www-data:www-data /var/www/prosushimsg
```

#### **Вариант Б: Через SCP (без Git):**

```powershell
# С твоего компьютера (Windows PowerShell)
cd C:\Users\user\source\repos\prosushimsg

# Создаём архив
Compress-Archive -Path prosushimsg, ProSushiMsg.Client -DestinationPath prosushimsg.zip

# Копируем на VPS
scp prosushimsg.zip root@YOUR_VPS_IP:/var/www/

# На VPS распаковываем
ssh root@YOUR_VPS_IP
cd /var/www
sudo apt install -y unzip
unzip prosushimsg.zip
```

---

### **ШАГ 5: СОБИРАЕМ ПРОЕКТ**

```bash
cd /var/www/prosushimsg

# Backend (ASP.NET Core)
cd prosushimsg
dotnet publish -c Release -o /var/www/prosushimsg/publish

# Frontend (Blazor WASM)
cd ../ProSushiMsg.Client
dotnet publish -c Release -o /var/www/prosushimsg/publish/wwwroot

# Проверяем файлы
ls /var/www/prosushimsg/publish
# Должно быть: prosushimsg.dll, wwwroot/, prosushi.db и т.д.
```

---

### **ШАГ 6: СОЗДАЁМ SYSTEMD SERVICE**

```bash
# Создаём файл сервиса
sudo nano /etc/systemd/system/prosushimsg.service
```

Вставляем:

```ini
[Unit]
Description=ProSushiMsg Chat Application
After=network.target

[Service]
Type=notify
WorkingDirectory=/var/www/prosushimsg/publish
ExecStart=/usr/bin/dotnet /var/www/prosushimsg/publish/prosushimsg.dll --urls "http://0.0.0.0:5000"
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=prosushimsg
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Сохраняем: `Ctrl+X` ? `Y` ? `Enter`

```bash
# Перезагружаем systemd
sudo systemctl daemon-reload

# Запускаем сервис
sudo systemctl start prosushimsg

# Проверяем статус
sudo systemctl status prosushimsg

# Должно быть: active (running)

# Автозапуск при перезагрузке
sudo systemctl enable prosushimsg
```

---

### **ШАГ 7: НАСТРАИВАЕМ NGINX (REVERSE PROXY)**

```bash
# Удаляем дефолтный конфиг
sudo rm /etc/nginx/sites-enabled/default

# Создаём новый конфиг
sudo nano /etc/nginx/sites-available/prosushimsg
```

Вставляем:

```nginx
# HTTP конфигурация
server {
    listen 80;
    server_name YOUR_DOMAIN_OR_IP;  # Замени на свой домен или IP

    # Лимиты
    client_max_body_size 100M;

    # Логи
    access_log /var/log/nginx/prosushimsg-access.log;
    error_log /var/log/nginx/prosushimsg-error.log;

    # Статика (Blazor WASM)
    location / {
        root /var/www/prosushimsg/publish/wwwroot;
        try_files $uri $uri/ /index.html;
        
        # Кеширование статики
        location ~* \.(js|css|wasm|png|jpg|jpeg|gif|ico|svg|woff2)$ {
            expires 1y;
            add_header Cache-Control "public, immutable";
        }
    }

    # API Backend (ASP.NET Core)
    location /api/ {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    # SignalR WebSocket
    location /chathub {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Таймауты для WebSocket
        proxy_read_timeout 3600s;
        proxy_send_timeout 3600s;
    }
}
```

Сохраняем: `Ctrl+X` ? `Y` ? `Enter`

```bash
# Активируем конфиг
sudo ln -s /etc/nginx/sites-available/prosushimsg /etc/nginx/sites-enabled/

# Проверяем синтаксис
sudo nginx -t

# Перезапускаем Nginx
sudo systemctl restart nginx
```

---

### **ШАГ 8: НАСТРОЙКА HTTPS (LET'S ENCRYPT)**

```bash
# Устанавливаем Certbot
sudo apt install -y certbot python3-certbot-nginx

# Получаем SSL сертификат
sudo certbot --nginx -d YOUR_DOMAIN

# Certbot автоматически:
# 1. Получит сертификат
# 2. Обновит Nginx конфиг
# 3. Добавит redirect с HTTP на HTTPS

# Проверяем автообновление
sudo certbot renew --dry-run
```

---

### **ШАГ 9: ОБХОД БЛОКИРОВОК (ДЛЯ РФ)**

#### **Вариант 1: Cloudflare Tunnel (бесплатно, лучший вариант)**

```bash
# Устанавливаем cloudflared
wget -q https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
sudo dpkg -i cloudflared-linux-amd64.deb

# Логинимся в Cloudflare
cloudflared tunnel login

# Создаём туннель
cloudflared tunnel create prosushimsg

# Создаём конфиг
mkdir -p ~/.cloudflared
nano ~/.cloudflared/config.yml
```

Вставляем:

```yaml
tunnel: <TUNNEL_ID из вывода выше>
credentials-file: /root/.cloudflared/<TUNNEL_ID>.json

ingress:
  - hostname: chat.yourdomain.com
    service: http://localhost:5000
  - service: http_status:404
```

```bash
# Маршрутизируем DNS
cloudflared tunnel route dns prosushimsg chat.yourdomain.com

# Запускаем как сервис
sudo cloudflared service install
sudo systemctl start cloudflared
sudo systemctl enable cloudflared
```

#### **Вариант 2: Hysteria2 (VPN, сложнее)**

```bash
# Устанавливаем Hysteria2
bash <(curl -fsSL https://get.hy2.sh/)

# Создаём конфиг
nano /etc/hysteria/config.yaml
```

```yaml
listen: :443

tls:
  cert: /etc/hysteria/server.crt
  key: /etc/hysteria/server.key

auth:
  type: password
  password: YOUR_STRONG_PASSWORD

masquerade:
  type: proxy
  proxy:
    url: https://www.google.com
    rewriteHost: true
```

```bash
# Генерируем самоподписанный сертификат
openssl req -x509 -nodes -newkey ec:<(openssl ecparam -name prime256v1) -keyout /etc/hysteria/server.key -out /etc/hysteria/server.crt -subj "/CN=bing.com" -days 36500

# Запускаем
sudo systemctl start hysteria-server
sudo systemctl enable hysteria-server
```

---

## ?? **МОНИТОРИНГ И ОБСЛУЖИВАНИЕ**

### **Просмотр логов:**

```bash
# Логи приложения
sudo journalctl -u prosushimsg -f

# Логи Nginx
sudo tail -f /var/log/nginx/prosushimsg-error.log
sudo tail -f /var/log/nginx/prosushimsg-access.log
```

### **Перезапуск сервиса:**

```bash
sudo systemctl restart prosushimsg
sudo systemctl restart nginx
```

### **Обновление приложения:**

```bash
cd /var/www/prosushimsg
sudo git pull
cd prosushimsg
dotnet publish -c Release -o /var/www/prosushimsg/publish
sudo systemctl restart prosushimsg
```

---

## ?? **МОНИТОРИНГ ПРОИЗВОДИТЕЛЬНОСТИ**

### **Установка Netdata (дашборд):**

```bash
bash <(curl -Ss https://my-netdata.io/kickstart.sh)

# Открываем порт
sudo ufw allow 19999

# Доступ: http://YOUR_VPS_IP:19999
```

### **Мониторинг через htop:**

```bash
sudo apt install -y htop
htop
```

---

## ??? **БЕЗОПАСНОСТЬ**

### **1. Отключить root SSH:**

```bash
sudo nano /etc/ssh/sshd_config
```

Меняем:
```
PermitRootLogin no
```

```bash
sudo systemctl restart sshd
```

### **2. Fail2Ban (защита от brute-force):**

```bash
sudo apt install -y fail2ban

sudo nano /etc/fail2ban/jail.local
```

```ini
[DEFAULT]
bantime = 3600
findtime = 600
maxretry = 5

[sshd]
enabled = true
```

```bash
sudo systemctl restart fail2ban
```

### **3. Регулярные обновления:**

```bash
sudo apt update && sudo apt upgrade -y
sudo reboot
```

---

## ?? **ИТОГОВАЯ АРХИТЕКТУРА**

```
???????????????????????????????????????????????????
?           ПОЛЬЗОВАТЕЛЬ (Браузер)                ?
???????????????????????????????????????????????????
                   ? HTTPS
                   ?
???????????????????????????????????????????????????
?              CLOUDFLARE TUNNEL                  ?
?        (обход блокировок, DDoS защита)          ?
???????????????????????????????????????????????????
                   ?
                   ?
???????????????????????????????????????????????????
?                 VPS СЕРВЕР                      ?
?  ?????????????????????????????????????????????  ?
?  ?           NGINX (Reverse Proxy)           ?  ?
?  ?   - Статика: /var/www/.../wwwroot        ?  ?
?  ?   - API: proxy_pass ? :5000               ?  ?
?  ?   - WebSocket: /chathub ? :5000           ?  ?
?  ?????????????????????????????????????????????  ?
?               ?             ?                    ?
?               ?             ?                    ?
?  ??????????????????  ??????????????????         ?
?  ?  ASP.NET Core  ?  ?   SignalR Hub  ?         ?
?  ?   :5000        ?  ?   (WebSocket)  ?         ?
?  ??????????????????  ??????????????????         ?
?           ?                                      ?
?           ?                                      ?
?  ??????????????????                             ?
?  ?  SQLite DB     ?                             ?
?  ?  prosushi.db   ?                             ?
?  ??????????????????                             ?
???????????????????????????????????????????????????
```

---

## ?? **СТОИМОСТЬ**

### **Минимальная конфигурация:**
- **VPS:** 1 CPU, 1 ГБ RAM, 10 ГБ диск = **199-600?/мес**
- **Домен:** .ru/.online/.space = **99-300?/год**
- **Cloudflare Tunnel:** **бесплатно**
- **Let's Encrypt SSL:** **бесплатно**

**Итого:** ~300-800?/мес

### **Рекомендуемая конфигурация (50-100 человек):**
- **VPS:** 2 CPU, 2 ГБ RAM, 40 ГБ диск = **500-1200?/мес**
- **Домен:** .com/.net = **600-1500?/год**
- **Cloudflare Pro (опционально):** $20/мес (~2000?)

**Итого:** ~700-3000?/мес

---

## ?? **ПРОВЕРКА РАБОТЫ**

```bash
# 1. Проверяем backend
curl http://localhost:5000/api/auth/login

# 2. Проверяем Nginx
curl http://YOUR_VPS_IP

# 3. Проверяем SSL
curl https://YOUR_DOMAIN

# 4. Проверяем WebSocket
# В браузере F12 ? Network ? WS ? должен быть /chathub
```

---

## ? **TROUBLESHOOTING**

### **Ошибка: "Connection refused"**
```bash
sudo systemctl status prosushimsg
# Если не запущен:
sudo journalctl -u prosushimsg -n 50
```

### **Ошибка: "502 Bad Gateway"**
```bash
# Backend не запущен или порт занят
sudo netstat -tulpn | grep 5000
sudo systemctl restart prosushimsg
```

### **Ошибка: "WebSocket connection failed"**
```bash
# Проверяем Nginx конфиг для /chathub
sudo nginx -t
sudo systemctl reload nginx
```

---

## ?? **ПОДДЕРЖКА**

Если что-то не работает:
1. Проверь логи: `sudo journalctl -u prosushimsg -f`
2. Проверь Nginx: `sudo nginx -t`
3. Проверь firewall: `sudo ufw status`
4. Ping меня с логами ??

---

## ?? **ГОТОВО!**

После всех шагов твой чат будет доступен по адресу:
- **HTTP:** http://YOUR_VPS_IP
- **HTTPS:** https://YOUR_DOMAIN

**SignalR реал-тайм работает!** ??????
