namespace CrowdCoffee.Api.Middleware;

/// <summary>
/// Story 16.1 (T16.1.1). HSTS is handled separately via the built-in UseHsts() (Program.cs,
/// production-only — it actively breaks local http:// dev if applied there), so this middleware
/// covers the rest of the AC's named headers. Content-Security-Policy is scoped to
/// non-Development only: Swagger UI (dev-only, Program.cs) serves its own inline
/// scripts/styles that a strict default-src 'self' CSP would block, and Swagger never runs in
/// production anyway (same `if (app.Environment.IsDevelopment())` guard that registers it).
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";

            if (!environment.IsDevelopment())
            {
                headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none'; object-src 'none'";
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
