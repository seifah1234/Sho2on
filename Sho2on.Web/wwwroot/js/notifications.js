// wwwroot/js/notifications.js

window.checkNotificationPermission = function () {
    if (!("Notification" in window)) return "unsupported";
    return Notification.permission;
};

window.dismissNotificationBanner = function () {
    const expiry = new Date();
    expiry.setDate(expiry.getDate() + 1);
    document.cookie = `notif_banner_dismissed=1; expires=${expiry.toUTCString()}; path=/`;
};

window.wasNotificationBannerDismissedRecently = function () {
    return document.cookie.split(';').some(c => c.trim().startsWith('notif_banner_dismissed='));
};

window.notificationService = {
    _audioContext: null,
    _sounds: {},
    _permissionChecked: false,

    // تهيئة AudioContext (مطلوب تفاعل مستخدم في البداية)
    initAudio: function () {
        try {
            // إنشاء AudioContext مرة واحدة وإعادة استخدامه
            if (!this._audioContext) {
                const AudioContext = window.AudioContext || window.webkitAudioContext;
                this._audioContext = new AudioContext();
                console.log('AudioContext initialized');
            }

            // لو الـ AudioContext متوقف (المتصفح وقفه لما الصفحة minimized)
            if (this._audioContext.state === 'suspended') {
                this._audioContext.resume();
            }
        } catch (e) {
            console.log('Web Audio API not supported:', e);
        }
    },

    // تحميل وتخزين الصوت مسبقاً
    preloadSound: async function (name, url) {
        if (!this._audioContext) return;

        try {
            const response = await fetch(url);
            const arrayBuffer = await response.arrayBuffer();
            const audioBuffer = await this._audioContext.decodeAudioData(arrayBuffer);
            this._sounds[name] = audioBuffer;
            console.log(`Sound loaded: ${name}`);
        } catch (e) {
            console.log(`Failed to load sound: ${name}`, e);
        }
    },

    // تشغيل الصوت (حتى لو المتصفح minimized)
    playSound: function (soundName) {
        // تجاهل الصوت الصامت
        if (soundName === 'silent' || soundName === 'none') {
            return;
        }

        // تجربة Web Audio API أولاً (أقوى وبيشتغل في الخلفية)
        if (this._audioContext && this._sounds[soundName]) {
            try {
                // استئناف الـ AudioContext لو متوقف
                if (this._audioContext.state === 'suspended') {
                    this._audioContext.resume();
                }

                const source = this._audioContext.createBufferSource();
                source.buffer = this._sounds[soundName];

                // إضافة gain node للتحكم في الصوت
                const gainNode = this._audioContext.createGain();
                gainNode.gain.value = 0.8;

                source.connect(gainNode);
                gainNode.connect(this._audioContext.destination);
                source.start(0);

                console.log(`Sound played: ${soundName}`);
                return;
            } catch (e) {
                console.log('Web Audio play failed, trying fallback:', e);
            }
        }

        // Fallback: استخدام HTML5 Audio
        this.playFallbackSound(soundName);
    },

    // Fallback باستخدام HTML5 Audio
    playFallbackSound: function (soundName) {
        const soundMap = {
            'default': '/sounds/notification.wav',
            'urgent': '/sounds/urgent.mp3',
            'message': '/sounds/message.mp3',
            'approval': '/sounds/approval.mp3',
            'rejection': '/sounds/rejection.mp3'
        };

        const soundUrl = soundMap[soundName] || soundMap['default'];

        try {
            const audio = new Audio(soundUrl);
            audio.volume = 0.8;

            // مهم: تشغيل الصوت حتى لو المتصفح minimized
            audio.play().then(() => {
                console.log(`Fallback sound played: ${soundName}`);
            }).catch(err => {
                console.log('Fallback audio failed:', err);

                // آخر محاولة: استخدام AudioContext مع oscillator (صوت تنبيه)
                this.playBeepSound();
            });
        } catch (e) {
            console.log('All audio methods failed:', e);
        }
    },

    // صوت تنبيه احتياطي (Beep) باستخدام Web Audio
    playBeepSound: function () {
        try {
            if (!this._audioContext) {
                this._audioContext = new (window.AudioContext || window.webkitAudioContext)();
            }

            if (this._audioContext.state === 'suspended') {
                this._audioContext.resume();
            }

            const oscillator = this._audioContext.createOscillator();
            const gainNode = this._audioContext.createGain();

            oscillator.connect(gainNode);
            gainNode.connect(this._audioContext.destination);

            oscillator.frequency.value = 800;
            oscillator.type = 'sine';
            gainNode.gain.value = 0.3;

            oscillator.start();

            // إيقاف الصوت بعد 200ms
            setTimeout(() => {
                oscillator.stop();
                gainNode.disconnect();
            }, 200);
        } catch (e) {
            console.log('Beep failed:', e);
        }
    },

    // طلب إذن الإشعارات
    requestPermission: async function () {
        if (!("Notification" in window)) {
            console.log("Notifications not supported");
            return false;
        }

        // لو الإذن موجود بالفعل
        if (Notification.permission === "granted") {
            this._permissionChecked = true;
            return true;
        }

        // لو الإذن اترفض
        if (Notification.permission === "denied") {
            this._permissionChecked = true;
            return false;
        }

        try {
            const permission = await Notification.requestPermission();
            this._permissionChecked = true;

            // لو المستخدم وافق، نشغل AudioContext
            if (permission === "granted") {
                this.initAudio();
            }

            return permission === "granted";
        } catch (error) {
            console.error("Permission error:", error);
            return false;
        }
    },

    // إظهار الإشعار مع الصوت
    showNotification: async function (title, message, icon, url, soundName) {
        // تشغيل الصوت أولاً (حتى لو مفيش إذن إشعارات)
        this.playSound(soundName || 'default');

        // إظهار System Notification لو الإذن موجود
        if (Notification.permission === "granted") {
            try {
                const options = {
                    body: message,
                    icon: icon || '/favicon.ico',
                    badge: '/favicon.ico',
                    tag: 'sho2on-notification-' + Date.now(), // tag فريد لكل إشعار
                    requireInteraction: true,
                    silent: false,
                    data: { url: url },
                    vibrate: [200, 100, 200] // اهتزاز للموبايل
                };

                const notification = new Notification(title, options);

                notification.onclick = function (event) {
                    event.preventDefault();
                    notification.close();

                    if (url) {
                        // فتح الرابط في نفس التبويب أو تبويب جديد
                        window.focus();
                        window.location.href = url;
                    }
                };

                // إغلاق الإشعار تلقائياً بعد 10 ثواني
                setTimeout(() => notification.close(), 10000);

                console.log('System notification shown');
            } catch (e) {
                console.log('System notification failed:', e);
            }
        } else {
            console.log('Notification permission not granted, showing in-app only');
        }
    }
};