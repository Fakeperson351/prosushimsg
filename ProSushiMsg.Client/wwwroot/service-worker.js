self.addEventListener('install', event => {
    console.log('[Service Worker] Installing...');
    const cacheName = 'prosushi-v1';
    event.waitUntil(
        caches.open(cacheName).then(cache => {
            return cache.addAll([
                '/',
                '/index.html',
                '/app.css',
                '/bootstrap.min.css'
            ]);
        })
    );
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    console.log('[Service Worker] Activating...');
    const cacheName = 'prosushi-v1';
    event.waitUntil(
        caches.keys().then(keys => {
            return Promise.all(
                keys.map(key => key !== cacheName ? caches.delete(key) : Promise.resolve())
            );
        })
    );
    self.clients.claim();
});

self.addEventListener('fetch', event => {
    const { request } = event;

    // Только GET requests
    if (request.method !== 'GET') {
        return;
    }

    // Пропускаем SignalR
    if (request.url.includes('/chathub')) {
        return;
    }

    // Network first для API
    if (request.url.includes('/api/')) {
        event.respondWith(
            fetch(request)
                .then(response => {
                    const clone = response.clone();
                    caches.open('prosushi-v1').then(cache => cache.put(request, clone));
                    return response;
                })
                .catch(() => caches.match(request))
        );
        return;
    }

    // Cache first для статики
    event.respondWith(
        caches.match(request)
            .then(response => response || fetch(request))
            .catch(() => new Response('Offline', { status: 503 }))
    );
});

// Background sync для offline messages
self.addEventListener('sync', event => {
    if (event.tag === 'sync-messages') {
        console.log('[Service Worker] Syncing messages...');
        event.waitUntil(syncMessages());
    }
});

async function syncMessages() {
    try {
        const pendingMessages = await getPendingMessages();
        for (const msg of pendingMessages) {
            await fetch('/api/messages/send', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(msg)
            });
            await clearPendingMessage(msg.id);
        }
    } catch (error) {
        console.error('Sync failed:', error);
        throw error;
    }
}

async function getPendingMessages() {
    const db = await openDB();
    return db.getAll('pending_messages');
}

async function clearPendingMessage(id) {
    const db = await openDB();
    return db.delete('pending_messages', id);
}

async function openDB() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open('prosushi_db', 1);
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result);
    });
}
