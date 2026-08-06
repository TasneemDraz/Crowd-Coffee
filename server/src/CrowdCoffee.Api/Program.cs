using CrowdCoffee.Api.Middleware;
using CrowdCoffee.Application.Cart;
using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Application.Common;
using CrowdCoffee.Application.Diagnostics;
using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Application.Payments;
using CrowdCoffee.Domain.Ordering;
using CrowdCoffee.Infrastructure.Configuration;
using CrowdCoffee.Infrastructure.Persistence;
using CrowdCoffee.Infrastructure.Persistence.Caching;
using CrowdCoffee.Infrastructure.Payments;
using CrowdCoffee.Infrastructure.Persistence.Repositories;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/crowdcoffee-.log", rollingInterval: RollingInterval.Day));

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<PaymobOptions>(builder.Configuration.GetSection(PaymobOptions.SectionName));

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

app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
