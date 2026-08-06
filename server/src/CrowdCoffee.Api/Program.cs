using CrowdCoffee.Api.Hubs;
using CrowdCoffee.Api.Middleware;
using CrowdCoffee.Application.Auth;
using CrowdCoffee.Application.Cart;
using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Application.Common;
using CrowdCoffee.Application.Diagnostics;
using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Application.Kitchen;
using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Application.Payments;
using CrowdCoffee.Domain.Ordering;
using CrowdCoffee.Domain.Staff;
using CrowdCoffee.Infrastructure.Auth;
using CrowdCoffee.Infrastructure.Configuration;
using CrowdCoffee.Infrastructure.Persistence;
using CrowdCoffee.Infrastructure.Persistence.Caching;
using CrowdCoffee.Infrastructure.Payments;
using CrowdCoffee.Infrastructure.Persistence.Repositories;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/crowdcoffee-.log", rollingInterval: RollingInterval.Day));

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<PaymobOptions>(builder.Configuration.GetSection(PaymobOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

var connectionString = builder.Configuration.GetSection(DatabaseOptions.SectionName)["ConnectionString"]
    ?? throw new InvalidOperationException("Database:ConnectionString is not configured.");

builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<IDomainEventHandler<OrderCancelledEvent>, LoggingOrderCancelledHandler>();
builder.Services.AddScoped<IDomainEventHandler<OrderCancelledEvent>, ReleaseStockOnOrderCancelledHandler>();
builder.Services.AddScoped<IDomainEventHandler<OrderCompletedEvent>, FulfillStockOnOrderCompletedHandler>();

builder.Services.AddDbContext<CrowdCoffeeDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddValidatorsFromAssemblyContaining<EchoRequestValidator>();
builder.Services.AddScoped<EchoHandler>();

builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<ReconcileStockLedgerHandler>();

builder.Services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();

builder.Services.AddScoped<IOrderStockRequirementRepository, OrderStockRequirementRepository>();
builder.Services.AddScoped<StockReservationService>();

builder.Services.AddScoped<RecordPurchaseHandler>();
builder.Services.AddScoped<RecordWasteHandler>();
builder.Services.AddScoped<RecordAdjustmentHandler>();
builder.Services.AddScoped<GetLowStockItemsHandler>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICatalogCache, MemoryCatalogCache>();

builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
builder.Services.AddScoped<GetCategoriesHandler>();
builder.Services.AddScoped<GetProductsHandler>();
builder.Services.AddScoped<GetProductModifiersHandler>();

builder.Services.AddScoped<RepriceCartHandler>();

builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderNumberGenerator, SqlOrderNumberGenerator>();
builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddScoped<GetOrderHandler>();
builder.Services.AddScoped<AdvanceOrderStatusHandler>();

builder.Services.AddScoped<GetKitchenBoardOrdersHandler>();
builder.Services.AddScoped<IDomainEventHandler<OrderStatusChangedEvent>, BroadcastOrderStatusChangedHandler>();
builder.Services.AddSignalR();

builder.Services.AddHttpClient<IPaymentGateway, PaymobGateway>((sp, client) =>
{
    var paymobOptions = sp.GetRequiredService<IOptions<PaymobOptions>>().Value;
    client.BaseAddress = new Uri(paymobOptions.ApiBaseUrl);
});
builder.Services.AddScoped<IPaymentEventStore, PaymentEventStore>();
builder.Services.AddScoped<InitiatePaymentHandler>();
builder.Services.AddScoped<HandlePaymentWebhookHandler>();
builder.Services.AddScoped<GetReconciliationHandler>();
builder.Services.AddHostedService<PaymentTimeoutJob>();

// §12: rate limiting on order creation and any future OTP/phone endpoint (T9.4.1). Partitioned
// per client IP so one abusive caller can't exhaust the limit for everyone else; a double-tapped
// Place Order button from a single customer is handled by Story 9.3's idempotency key, not this —
// this guards against scripted abuse (fake-order spam eating stock reservations/gateway calls).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("order-creation", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 5,
            QueueLimit = 0
        }));

    // Blunts credential-stuffing/brute-force against staff login specifically — separate from
    // "order-creation" since a legitimate barista mistyping a password a few times shouldn't be
    // throttled by a limit sized for anonymous customer traffic.
    options.AddPolicy("staff-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 10,
            QueueLimit = 0
        }));
});

builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IStaffUserRepository, StaffUserRepository>();
builder.Services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<RefreshHandler>();
builder.Services.AddScoped<LogoutHandler>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Browsers can't set a custom Authorization header on the WebSocket upgrade SignalR's
        // client uses, so its accessTokenFactory sends the JWT as ?access_token=... instead
        // (Epic 12) — bridge that into the normal bearer-token path, scoped to hub paths only so
        // a leaked query-string token elsewhere in logs can't be mistaken for the real mechanism.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManagerOnly", policy => policy.RequireRole(nameof(StaffRole.Manager)));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole(nameof(StaffRole.Manager), nameof(StaffRole.Barista)));
});

builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "sql-server");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<OrderHub>("/hubs/orders");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
