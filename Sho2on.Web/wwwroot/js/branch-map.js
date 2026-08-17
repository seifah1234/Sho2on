window.branchMap = {
    map: null,
    marker: null,
    circle: null,
    currentLat: null,
    currentLng: null,
    currentRadius: 100,

    init: function (elementId, lat, lng, radius, dotNetHelper) {
        // تنظيف أي خريطة سابقة
        this.destroy();

        // تخزين القيم الحالية
        this.currentLat = lat;
        this.currentLng = lng;
        this.currentRadius = radius || 100;

        // القاهرة كموقع افتراضي
        const defaultLat = lat ?? 30.0444;
        const defaultLng = lng ?? 31.2357;

        // التأكد من أن العنصر موجود ومرئي
        const element = document.getElementById(elementId);
        if (!element) {
            console.error('Map element not found:', elementId);
            return;
        }

        // إنشاء الخريطة
        this.map = L.map(elementId, {
            center: [defaultLat, defaultLng],
            zoom: lat && lng ? 16 : 13,
            zoomControl: true,
            scrollWheelZoom: true
        });

        // إضافة طبقة OpenStreetMap
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(this.map);

        // إضافة الموقع إذا كان موجوداً
        if (lat != null && lng != null) {
            this.setLocation(lat, lng, this.currentRadius);
        }

        // معالجة النقر على الخريطة
        this.map.on('click', (e) => {
            const newLat = e.latlng.lat;
            const newLng = e.latlng.lng;

            this.currentLat = newLat;
            this.currentLng = newLng;

            this.setLocation(newLat, newLng, this.currentRadius);

            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('LocationChanged', newLat, newLng)
                    .catch(err => console.error('Error calling .NET method:', err));
            }
        });

        // تصحيح حجم الخريطة بعد ظهور الـ modal
        setTimeout(() => {
            if (this.map) {
                this.map.invalidateSize();

                if (this.currentLat != null && this.currentLng != null) {
                    this.map.setView([this.currentLat, this.currentLng], 16);
                } else {
                    this.map.setView([defaultLat, defaultLng], 13);
                }
            }
        }, 500);
    },

    setLocation: function (lat, lng, radius) {
        if (!this.map) return;

        // تحديث القيم الحالية
        this.currentLat = lat;
        this.currentLng = lng;
        this.currentRadius = radius || 100;

        // إزالة العناصر القديمة
        if (this.marker) {
            this.map.removeLayer(this.marker);
        }
        if (this.circle) {
            this.map.removeLayer(this.circle);
        }

        // إضافة علامة جديدة
        this.marker = L.marker([lat, lng], {
            draggable: true
        }).addTo(this.map);

        // تحديث الموقع عند سحب العلامة
        this.marker.on('dragend', () => {
            const position = this.marker.getLatLng();
            this.currentLat = position.lat;
            this.currentLng = position.lng;
            this.map.setView([position.lat, position.lng], this.map.getZoom());

            // تحديث القيم في Blazor
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('LocationChanged', position.lat, position.lng)
                    .catch(err => console.error('Error calling .NET method:', err));
            }
        });

        // إضافة دائرة نطاق الحضور
        this.circle = L.circle([lat, lng], {
            radius: this.currentRadius,
            color: '#4CAF50',
            fillColor: '#4CAF50',
            fillOpacity: 0.2,
            weight: 2
        }).addTo(this.map);

        // تحديث عرض الخريطة
        this.map.setView([lat, lng], 16);
    },

    setRadius: function (radius) {
        this.currentRadius = Number(radius) || 100;

        if (this.circle) {
            this.circle.setRadius(this.currentRadius);
        }
    },

    getCurrentLocation: function () {
        return {
            lat: this.currentLat,
            lng: this.currentLng,
            radius: this.currentRadius
        };
    },

    invalidateSize: function () {
        if (this.map) {
            setTimeout(() => {
                this.map.invalidateSize();
            }, 100);
        }
    },

    destroy: function () {
        if (this.map) {
            this.map.remove();
            this.map = null;
            this.marker = null;
            this.circle = null;
            this.currentLat = null;
            this.currentLng = null;
            this.currentRadius = 100;
        }
    }
};