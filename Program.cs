// ============================================================================
// Program.cs
//
// Purpose: Application entry point and startup configuration
// Framework: ASP.NET Core 8.0 Blazor Server
//
// Responsibilities:
// 1. Configure Dependency Injection (DI) container
// 2. Register services (WeatherService, LocalStorageService, FavoritesService)
// 3. Configure HTTP client with Polly resilience policies
// 4. Set up middleware pipeline (HTTPS, static files, antiforgery, routing)
// 5. Configure Blazor Server interactive rendering
// 6. Map routes and fallback handler
//
// Key Features:
// - Memory caching for API responses
// - HTTP client with retry policy (exponential backoff)
// - Circuit breaker pattern to prevent cascading failures
// - Development vs Production environment configuration
// - Fallback route to handle 404s (redirects to homepage)
//
// Dependencies:
// - Polly: Resilience and transient-fault-handling library
// - WeatherDashboard.Services: Custom service implementations
// - WeatherDashboard.Components: Blazor components
// ============================================================================

using Polly;
using Polly.Extensions.Http;
using WeatherDashboard.Components;
using WeatherDashboard.Services;

namespace WeatherDashboard
{
    // Main Program class - entry point for ASP.NET Core application
    public class Program
    {
        // Main method - called when application starts
        // Parameters: args - command line arguments (not used in this app)
        public static void Main(string[] args)
        {
            // ======= APPLICATION BUILDER SETUP =======

            // Step 1: Create web application builder
            // This configures the host, services, and middleware
            var builder = WebApplication.CreateBuilder(args);

            // ======= SERVICE REGISTRATION =======
            // Services registered here are available via Dependency Injection
            // Scoped services: Created once per HTTP request/SignalR connection

            // Step 2: Add Razor Components (Blazor) support
            // AddRazorComponents: Registers core Blazor services
            // AddInteractiveServerComponents: Enables Blazor Server (SignalR-based) interactivity
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Step 3: Add in-memory cache for storing API responses
            // Used by WeatherService to cache weather data (5 minutes)
            // Reduces API calls and improves performance
            builder.Services.AddMemoryCache();

            // Step 4: Register HTTP client for WeatherService with Polly resilience policies
            // AddHttpClient<T>: Creates typed HttpClient for WeatherService
            // AddPolicyHandler: Applies Polly policies to all HTTP requests
            //   - Retry Policy: Automatically retries failed requests (3 times, exponential backoff)
            //   - Circuit Breaker: Opens circuit after 5 failures, prevents cascading failures
            builder.Services.AddHttpClient<WeatherService>()
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());

            // Step 5: Register WeatherService as scoped service
            // Scoped: One instance per HTTP request/SignalR connection
            // Used by components to fetch weather data from Open-Meteo API
            builder.Services.AddScoped<WeatherService>();

            // Step 6: Register LocalStorage and Favorites services
            // LocalStorageService: JavaScript interop wrapper for browser localStorage
            // FavoritesService: Manages favorite cities using LocalStorageService
            // Both are scoped: One instance per connection
            builder.Services.AddScoped<LocalStorageService>();
            builder.Services.AddScoped<FavoritesService>();

            // ======= APPLICATION BUILDER BUILD =======

            // Step 7: Build the application from configured services
            // After this point, no more services can be registered
            var app = builder.Build();

            // ======= MIDDLEWARE PIPELINE CONFIGURATION =======
            // Middleware executes in order, forming a request/response pipeline
            // Request: Top to bottom, Response: Bottom to top

            // Step 8: Configure error handling and HSTS (production only)
            if (!app.Environment.IsDevelopment())
            {
                // Production/Staging environment configuration

                // UseExceptionHandler: Global error handler
                // Redirects unhandled exceptions to /Error page (Error.razor)
                app.UseExceptionHandler("/Error");

                // UseHsts: HTTP Strict Transport Security
                // Forces browsers to use HTTPS for 30 days
                // Prevents man-in-the-middle attacks
                // Note: Not enabled in development to avoid localhost issues
                app.UseHsts();
            }

            // Step 9: Redirect HTTP requests to HTTPS
            // Ensures all traffic is encrypted
            app.UseHttpsRedirection();

            // Step 10: Enable static file serving
            // Serves files from wwwroot folder (CSS, JS, images)
            // Examples: /app.css, /js/theme.js
            app.UseStaticFiles();

            // Step 11: Enable antiforgery protection
            // Prevents Cross-Site Request Forgery (CSRF) attacks
            // Automatically validates antiforgery tokens on form submissions
            app.UseAntiforgery();

            // ======= ROUTE MAPPING =======

            // Step 12: Map Razor Components (Blazor) to routes
            // MapRazorComponents<App>: Sets App.razor as the root component
            // AddInteractiveServerRenderMode: Enables Blazor Server interactivity
            // This allows components to use @page directives for routing
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Step 13: Fallback route for unmatched URLs
            // MapFallback: Catches all routes that don't match any component
            // Redirects to homepage ("/") instead of showing 404 error
            // This ensures users always see the app, even with invalid URLs
            app.MapFallback(context =>
            {
                // Redirect to homepage
                context.Response.Redirect("/");

                // Return completed task (async operation complete)
                return Task.CompletedTask;
            });

            // ======= START APPLICATION =======

            // Step 14: Start the web application
            // This blocks until the application shuts down
            // Starts Kestrel web server and begins listening for requests
            app.Run();
        }

        // ======= POLLY RESILIENCE POLICIES =======

        // Get retry policy for HTTP requests
        // Returns: Polly policy that retries failed HTTP requests
        //
        // How it works:
        // 1. Detects transient HTTP errors (5xx server errors, timeouts, network issues)
        // 2. Retries request up to 3 times with exponential backoff
        // 3. Wait times: 2s (1st retry), 4s (2nd retry), 8s (3rd retry)
        //
        // Why exponential backoff?
        // - Gives server time to recover from temporary issues
        // - Prevents overwhelming server with immediate retries
        // - Industry best practice for API resilience
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                // HandleTransientHttpError: Detects temporary failures
                // Includes: HTTP 5xx, HTTP 408, network exceptions
                .HandleTransientHttpError()
                // WaitAndRetryAsync: Retry 3 times with exponential backoff
                // Parameters:
                //   - 3: Number of retry attempts
                //   - retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)):
                //     Calculates wait time: 2^1=2s, 2^2=4s, 2^3=8s
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        // Get circuit breaker policy for HTTP requests
        // Returns: Polly policy that prevents cascading failures
        //
        // How it works:
        // 1. Monitors HTTP request failures
        // 2. After 5 consecutive failures, "opens" the circuit (stops making requests)
        // 3. Circuit stays open for 30 seconds (gives API time to recover)
        // 4. After 30 seconds, circuit enters "half-open" state (allows 1 test request)
        // 5. If test request succeeds, circuit closes (normal operation resumes)
        // 6. If test request fails, circuit opens again for another 30 seconds
        //
        // Why circuit breaker?
        // - Prevents wasting resources on failing API
        // - Gives failing service time to recover
        // - Fails fast instead of making user wait for timeout
        // - Protects both client and server from overload
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                // HandleTransientHttpError: Detects temporary failures
                .HandleTransientHttpError()
                // CircuitBreakerAsync: Opens circuit after threshold
                // Parameters:
                //   - 5: Number of consecutive failures before opening circuit
                //   - TimeSpan.FromSeconds(30): Duration circuit stays open
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        }
    }
}
