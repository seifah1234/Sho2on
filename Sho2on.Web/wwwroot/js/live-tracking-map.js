window.liveTrackingMap = {
    map: null,
    markers: {}, // userId -> L.marker
    dotNetHelper: null,

    init: function (elementId, employees, dotNetHelper) {
        this.destroy();
        this.dotNetHelper = dotNetHelper;

        const element = document.getElementById(elementId);
        if (!element) {
            console.error('Map element not found:', elementId);
            return;
        }

        const defaultLat = 30.0444; // القاهرة كمركز افتراضي
        const defaultLng = 31.2357;

        this.map = L.map(elementId, {
            center: [defaultLat, defaultLng],
            zoom: 12,
            zoomControl: true,
            scrollWheelZoom: true
        });

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(this.map);

        this.updateMarkers(employees);

        setTimeout(() => {
            if (this.map) this.map.invalidateSize();
        }, 300);
    },

    _iconFor: function (isActive) {
        const color = isActive ? '#10B981' : '#9CA3AF';
        return L.divIcon({
            className: 'live-tracking-marker',
            html: `<div style="
                width:28px;height:28px;border-radius:50%;
                background:${color};border:3px solid #fff;
                box-shadow:0 2px 6px rgba(0,0,0,.35);
                display:flex;align-items:center;justify-content:center;
                color:#fff;font-size:14px;">
                <i class="bi bi-person-fill"></i>
            </div>`,
            iconSize: [28, 28],
            iconAnchor: [14, 14]
        });
    },

    updateMarkers: function (employees) {
        if (!this.map) return;

        const seenIds = new Set();

        employees.forEach(emp => {
            seenIds.add(emp.userId);
            const latLng = [emp.latitude, emp.longitude];
            const icon = this._iconFor(emp.isRecentlyActive);

            if (this.markers[emp.userId]) {
                this.markers[emp.userId].setLatLng(latLng);
                this.markers[emp.userId].setIcon(icon);
                this.markers[emp.userId].setPopupContent(this._popupHtml(emp));
            } else {
                const marker = L.marker(latLng, { icon: icon }).addTo(this.map);
                marker.bindPopup(this._popupHtml(emp));
                marker.on('click', () => {
                    if (this.dotNetHelper) {
                        this.dotNetHelper.invokeMethodAsync('OnMarkerClicked', emp.userId)
                            .catch(err => console.error(err));
                    }
                });
                this.markers[emp.userId] = marker;
            }
        });

        // شيل الموظفين اللي مش موجودين في التحديث الجديد
        Object.keys(this.markers).forEach(id => {
            if (!seenIds.has(Number(id))) {
                this.map.removeLayer(this.markers[id]);
                delete this.markers[id];
            }
        });
    },

    _popupHtml: function (emp) {
        return `<div style="font-family:Tajawal, sans-serif; text-align:right; min-width:150px;">
            <strong>${emp.fullName}</strong><br/>
            <span style="color:#6B7280;font-size:12px;">${emp.jobTitleName ?? ''}</span><br/>
            <span style="font-size:11px;color:${emp.isRecentlyActive ? '#10B981' : '#9CA3AF'};">
                ${emp.isRecentlyActive ? 'أونلاين الآن' : 'غير نشط'}
            </span>
        </div>`;
    },

    focusOn: function (userId, lat, lng) {
        if (!this.map) return;
        this.map.setView([lat, lng], 16);
        const marker = this.markers[userId];
        if (marker) marker.openPopup();
    },

    invalidateSize: function () {
        if (this.map) {
            setTimeout(() => this.map.invalidateSize(), 100);
        }
    },

    destroy: function () {
        if (this.map) {
            this.map.remove();
            this.map = null;
            this.markers = {};
            this.dotNetHelper = null;
        }
    }
};
