// ============================================================================
// theme.js
//
// Purpose: Client-side theme management for light/dark mode toggle
// Usage: Called from Blazor C# code via JavaScript Interop (IJSRuntime)
// Storage: Theme preference stored in browser's localStorage
//
// How it works:
// 1. On page load: Initialize theme from localStorage (or default to light)
// 2. User clicks theme toggle: Blazor calls setTheme() to save & apply new theme
// 3. Theme applied via data-theme attribute on <body> element
// 4. CSS variables in app.css respond to [data-theme="dark"] selector
//
// Functions exposed to Blazor via window.themeManager:
// - getTheme(): Returns current theme from localStorage ("light" or "dark")
// - setTheme(theme): Saves theme to localStorage and applies to DOM
// - initializeTheme(): Loads and applies saved theme on page load
// ============================================================================

// Global themeManager object - accessible from Blazor via JSInterop
// Blazor calls these functions using: JSRuntime.InvokeAsync("themeManager.getTheme")
window.themeManager = {
    // Get current theme preference from browser's localStorage
    // Returns: "light" or "dark" (defaults to "light" if nothing stored)
    // Called by: ThemeToggle.razor on component initialization
    getTheme: function () {
        // Try to get theme from localStorage, fallback to "light" if not found
        // localStorage.getItem returns null if key doesn't exist
        return localStorage.getItem('theme') || 'light';
    },

    // Save theme preference to localStorage and apply to DOM
    // Parameters: theme - "light" or "dark" theme name
    // Called by: ThemeToggle.razor when user clicks theme toggle button
    setTheme: function (theme) {
        // Step 1: Persist theme choice to browser's localStorage
        // This allows theme to persist across page reloads and browser sessions
        localStorage.setItem('theme', theme);

        // Step 2: Apply theme to DOM by setting data-theme attribute on <body>
        // CSS rules in app.css target body[data-theme="dark"] to apply dark styles
        // CSS variables (--bg-primary, --text-primary, etc.) update automatically
        document.body.setAttribute('data-theme', theme);
    },

    // Initialize theme on page load
    // Loads saved theme from localStorage and applies to DOM
    // Called by: DOMContentLoaded event listener below
    initializeTheme: function () {
        // Step 1: Get saved theme from localStorage (or default "light")
        const theme = this.getTheme();

        // Step 2: Apply theme to DOM immediately
        // This prevents flash of wrong theme on page load
        document.body.setAttribute('data-theme', theme);
    }
};

// Page load event listener - runs after HTML is fully loaded
// DOMContentLoaded fires before images/stylesheets finish loading (faster than window.onload)
document.addEventListener('DOMContentLoaded', function () {
    // Initialize theme as soon as DOM is ready
    // This ensures correct theme is applied before user sees the page
    // Prevents "flash of light mode" when user has dark mode saved
    window.themeManager.initializeTheme();
});
