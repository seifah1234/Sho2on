window.dashboardCharts = {};

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