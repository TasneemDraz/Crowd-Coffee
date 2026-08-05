using CrowdCoffee.Api.Middleware;
using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Application.Common;
using CrowdCoffee.Application.Diagnostics;
using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Domain.Ordering;
using CrowdCoffee.Infrastructure.Configuration;
using CrowdCoffee.Infrastructure.Persistence;
using CrowdCoffee.Infrastructure.Persistence.Caching;
using CrowdCoffee.Infrastructure.Persistence.Repositories;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/crowdcoffee-.log", rollingInterval: RollingInterval.Day));

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));

var connectionString = builder.Configuration.GetSection(DatabaseOptions.SectionName)["ConnectionString"]
    ?? throw new InvalidOperationException("Database:ConnectionString is not configured.");

builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<IDomainEventHandler<OrderCancelledEvent>, LoggingOrderCancelledHandler>();
builder.Services.AddScoped<IDomainEventHandler<OrderCancelledEvent>, ReleaseStockOnOrderCancelledHandler>();
builder.Services.AddScoped<IDomainEventHandler<OrderCompletedEvent>, FulfillStockOnOrderCompletedHandler>();

builder.Services.AddDbContext<CrowdCoffeeDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddControllers();
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

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
