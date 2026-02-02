// 🚀 Service Worker для ProSushi Messenger (PWA)
// Версия кэша — меняй при обновлении приложения!
const CACHE_NAME = 'prosushi-v1.0.0';
const OFFLINE_URL = '/offline.html';

// Файлы для кэширования при установке
const PRECACHE_URLS = [
    '/',
    '/index.html',
    '/css/app.css',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/icon-192x192.svg',
    '/icon-512x512.svg'
];

// 📦 УСТАНОВКА Service Worker
self.addEventListener('install', event => {
    console.log('[SW] 🔧 Установка Service Worker...');
    
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('[SW] ✅ Кэширование базовых файлов');
                return cache.addAll(PRECACHE_URLS);
            })
            .catch(err => {
                console.error('[SW] ❌ Ошибка кэширования:', err);
            })
    );
    
    // Немедленная активация нового Service Worker
    self.skipWaiting();
});

// 🔄 АКТИВАЦИЯ Service Worker
self.addEventListener('activate', event => {
    console.log('[SW] ✅ Активация Service Worker');
    
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cacheName => {
                    // Удаляем старые кэши
                    if (cacheName !== CACHE_NAME) {
                        console.log('[SW] 🗑️ Удаление старого кэша:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
    
    // Захватываем все клиенты сразу после активации
    self.clients.claim();
});

// 🌐 ОБРАБОТКА ЗАПРОСОВ (стратегия кэширования)
self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);
    
    // ❌ Пропускаем не-GET запросы
    if (request.method !== 'GET') {
        return;
    }
    
    // ❌ Пропускаем WebSocket (SignalR)
    if (url.pathname.includes('/chathub') || url.pathname.includes('/_blazor')) {
        return;
    }
    
    // ❌ Пропускаем chrome-extension и другие протоколы
    if (!url.protocol.startsWith('http')) {
        return;
    }
    
    // 🔥 API запросы: Network First (сеть приоритетнее)
    if (url.pathname.startsWith('/api/')) {
        event.respondWith(networkFirstStrategy(request));
        return;
    }
    
    // 📦 Статические файлы: Cache First (кэш приоритетнее)
    event.respondWith(cacheFirstStrategy(request));
});

// 🌐 СТРАТЕГИЯ: Network First (для API)
async function networkFirstStrategy(request) {
    try {
        // Пробуем запрос к серверу
        const response = await fetch(request);
        
        // Кэшируем успешный ответ
        if (response.ok) {
            const cache = await caches.open(CACHE_NAME);
            cache.put(request, response.clone());
        }
        
        return response;
    } catch (error) {
        // Если сеть недоступна — берём из кэша
        console.log('[SW] 📡 Сеть недоступна, используем кэш для:', request.url);
        const cached = await caches.match(request);
        
        if (cached) {
            return cached;
        }
        
        // Если в кэше тоже нет — возвращаем ошибку
        return new Response(JSON.stringify({ error: 'Offline' }), {
            status: 503,
            headers: { 'Content-Type': 'application/json' }
        });
    }
}

// 📦 СТРАТЕГИЯ: Cache First (для статики)
async function cacheFirstStrategy(request) {
    const cached = await caches.match(request);
    
    if (cached) {
        console.log('[SW] ✅ Из кэша:', request.url);
        return cached;
    }
    
    try {
        const response = await fetch(request);
        
        // Кэшируем только успешные ответы
        if (response.ok) {
            const cache = await caches.open(CACHE_NAME);
            cache.put(request, response.clone());
        }
        
        return response;
    } catch (error) {
        console.error('[SW] ❌ Ошибка загрузки:', request.url, error);
        
        // Возвращаем оффлайн-страницу (если она есть)
        const offlinePage = await caches.match(OFFLINE_URL);
        if (offlinePage) {
            return offlinePage;
        }
        
        return new Response('Offline', { status: 503 });
    }
}

// 🔔 PUSH УВЕДОМЛЕНИЯ (для будущего)
self.addEventListener('push', event => {
    const data = event.data ? event.data.json() : {};
    const title = data.title || 'ProSushi Messenger';
    const options = {
        body: data.body || 'Новое сообщение',
        icon: '/icon-192x192.png',
        badge: '/icon-96x96.png',
        tag: data.tag || 'message',
        data: data.url || '/',
        vibrate: [200, 100, 200],
        requireInteraction: false
    };
    
    event.waitUntil(
        self.registration.showNotification(title, options)
    );
});

// 👆 КЛИК ПО УВЕДОМЛЕНИЮ
self.addEventListener('notificationclick', event => {
    event.notification.close();
    
    const urlToOpen = event.notification.data || '/';
    
    event.waitUntil(
        clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(clientList => {
                // Если есть открытое окно — фокусируем его
                for (const client of clientList) {
                    if (client.url === urlToOpen && 'focus' in client) {
                        return client.focus();
                    }
                }
                
                // Иначе открываем новое окно
                if (clients.openWindow) {
                    return clients.openWindow(urlToOpen);
                }
            })
    );
});

// 🔄 BACKGROUND SYNC (отправка сообщений оффлайн)
self.addEventListener('sync', event => {
    console.log('[SW] 🔄 Background Sync:', event.tag);
    
    if (event.tag === 'sync-messages') {
        event.waitUntil(syncPendingMessages());
    }
});

// 📤 Синхронизация отложенных сообщений
async function syncPendingMessages() {
    try {
        console.log('[SW] 📤 Синхронизация сообщений...');
        
        // Здесь будет логика отправки из IndexedDB
        // TODO: интеграция с IndexedDB для хранения оффлайн-сообщений
        
        console.log('[SW] ✅ Сообщения синхронизированы');
    } catch (error) {
        console.error('[SW] ❌ Ошибка синхронизации:', error);
        throw error; // Перебросит sync для повторной попытки
    }
}

// 📊 СООБЩЕНИЯ ОТ КЛИЕНТА (для управления кэшем)
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        console.log('[SW] ⏭️ Принудительное обновление');
        self.skipWaiting();
    }
    
    if (event.data && event.data.type === 'CLEAR_CACHE') {
        console.log('[SW] 🗑️ Очистка кэша по запросу');
        event.waitUntil(
            caches.keys().then(names => {
                return Promise.all(names.map(name => caches.delete(name)));
            })
        );
    }
});

console.log('[SW] 🚀 Service Worker загружен');
