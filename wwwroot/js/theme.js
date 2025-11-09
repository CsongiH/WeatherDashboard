window.themeManager = {
    getTheme: function () {
        return localStorage.getItem('theme') || 'light';
    },
    setTheme: function (theme) {
        localStorage.setItem('theme', theme);
        document.body.setAttribute('data-theme', theme);
    },
    initializeTheme: function () {
        const theme = this.getTheme();
        document.body.setAttribute('data-theme', theme);
    }
};

// Initialize theme on page load
document.addEventListener('DOMContentLoaded', function () {
    window.themeManager.initializeTheme();
});
