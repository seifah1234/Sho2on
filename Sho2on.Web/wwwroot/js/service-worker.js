// wwwroot/service-worker.js
self.addEventListener('push', function (event) {
    const data = event.data.json();

    const options = {
        body: data.message,
        icon: data.icon || '/favicon.ico',
        badge: '/favicon.ico',
        tag: 'sho2on-notification',
        requireInteraction: true,
        data: {
            url: data.url
        }
    };

    event.waitUntil(
        self.registration.showNotification(data.title, options)
    );
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();

    if (event.notification.data.url) {
        clients.openWindow(event.notification.data.url);
    }
});