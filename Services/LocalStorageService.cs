// ============================================================================
// LocalStorageService.cs
//
// Purpose: C# wrapper service for browser localStorage access via JavaScript Interop
// Usage: Provides async methods to interact with browser's localStorage
// Dependencies: Microsoft.JSInterop for calling JavaScript from Blazor
//
// Why this exists: Blazor Server runs on the server, not in the browser.
// To access browser localStorage, we must use JSInterop to call JavaScript functions.
// ============================================================================

using Microsoft.JSInterop;

namespace WeatherDashboard.Services
{
    // Service for accessing browser localStorage from Blazor Server C# code
    // Provides a safe, async wrapper around JavaScript localStorage API
    public class LocalStorageService
    {
        // JavaScript runtime for invoking browser JavaScript functions from C#
        private readonly IJSRuntime _jsRuntime;

        // Constructor: Inject IJSRuntime via dependency injection
        // Parameters: jsRuntime - Blazor's JavaScript interop runtime
        public LocalStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        // Get a value from browser localStorage by key
        // Parameters: key - the localStorage key to retrieve
        // Returns: The stored value as string, or null if not found or error occurs
        //
        // Error handling: Returns null instead of throwing exceptions
        // Common failure reasons:
        //   - localStorage is disabled (browser privacy mode)
        //   - Browser doesn't support localStorage
        //   - Key doesn't exist in storage
        public async Task<string?> GetItemAsync(string key)
        {
            try
            {
                // Call JavaScript localStorage.getItem function via JSInterop
                // The browser's localStorage API is accessed from server-side C# code
                return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            }
            catch
            {
                // Silently fail and return null if localStorage access fails
                // This happens in private browsing mode or when localStorage is disabled
                return null;
            }
        }

        // Set a value in browser localStorage
        // Parameters:
        //   key - the localStorage key to set
        //   value - the string value to store
        //
        // Error handling: Silently fails without throwing exceptions
        // Common failure reasons:
        //   - localStorage is disabled (browser privacy mode)
        //   - Storage quota exceeded
        //   - Browser doesn't support localStorage
        public async Task SetItemAsync(string key, string value)
        {
            try
            {
                // Call JavaScript localStorage.setItem function via JSInterop
                // void async call - no return value expected
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
            }
            catch
            {
                // Silently fail if localStorage is not available
                // Graceful degradation - app continues working without persistence
            }
        }
    }
}
