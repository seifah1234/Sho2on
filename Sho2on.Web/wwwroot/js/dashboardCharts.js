window.dashboardCharts = {};

window.renderHiringTrendChart = function (canvasId, labels, hired, left) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.error('Canvas not found:', canvasId);
        return;
    }

    // امسح أي chart قديم
    if (canvas._chartInstance) {
        canvas._chartInstance.destroy();
    }

    const ctx = canvas.getContext('2d');
    canvas._chartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'تم تعيينهم',
                    data: hired,
                    backgroundColor: 'rgba(16, 185, 129, 0.7)',
                    borderColor: '#10B981',
                    borderWidth: 1,
                    borderRadius: 6,
                },
                {
                    label: 'غادروا',
                    data: left,
                    backgroundColor: 'rgba(239, 68, 68, 0.7)',
                    borderColor: '#EF4444',
                    borderWidth: 1,
                    borderRadius: 6,
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                    labels: {
                        font: { family: 'Tajawal', size: 13 },
                        usePointStyle: true,
                    }
                },
                tooltip: {
                    rtl: true,
                    textDirection: 'rtl',
                    titleFont: { family: 'Tajawal' },
                    bodyFont: { family: 'Tajawal' },
                }
            },
            scales: {
                x: {
                    grid: { display: false },
                    ticks: { font: { family: 'Tajawal', size: 11 } }
                },
                y: {
                    beginAtZero: true,
                    ticks: {
                        precision: 0,
                        font: { family: 'Tajawal' }
                    }
                }
            }
        }
    });
};

window.renderPieChart = function(canvasId, labels, data, colors) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        if (window.dashboardCharts[canvasId]) window.dashboardCharts[canvasId].destroy();
        window.dashboardCharts[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: { labels, datasets: [{ data, backgroundColor: colors, borderWidth: 0 }] },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { font: { family: 'Tajawal', size: 11 }, boxWidth: 10, padding: 8 }
                    }
                }
            }
        });
    };

    window.renderBarChart = function(canvasId, labels, data, color) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;
        if (window.dashboardCharts[canvasId]) window.dashboardCharts[canvasId].destroy();
        window.dashboardCharts[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: { labels, datasets: [{ data, backgroundColor: color, borderRadius: 6, maxBarThickness: 26 }] },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, ticks: { font: { size: 10 } } },
                    x: { ticks: { font: { size: 10 } } }
                }
            }
        });
    };