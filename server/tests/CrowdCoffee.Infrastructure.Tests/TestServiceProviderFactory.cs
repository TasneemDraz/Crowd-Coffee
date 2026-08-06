using CrowdCoffee.Application.Cart;
using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Application.Common;
using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Application.Kitchen;
using CrowdCoffee.Application.Ordering;
using CrowdCoffee.Application.Payments;
using CrowdCoffee.Domain.Ordering;
using CrowdCoffee.Infrastructure.Configuration;
using CrowdCoffee.Infrastructure.Payments;
using CrowdCoffee.Infrastructure.Persistence;
using CrowdCoffee.Infrastructure.Persistence.Caching;
using CrowdCoffee.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CrowdCoffee.Infrastructure.Tests;

/// <summary>
/// Points at the same local SQL Server dev database as `dotnet run` (no separate test DB exists
/// yet at this stage of the project) — tests are responsible for cleaning up whatever rows they
/// insert. Wires the same DI graph as Program.cs for the inventory slice, so these tests exercise
/// the real IDomainEventDispatcher pipeline, not a hand-rolled substitute.
/// </summary>
public static class TestServiceProviderFactory
{
    private const string ConnectionString = "Server=.;Database=CrowdCoffee;Trusted_Connection=True;TrustServerCertificate=True";

    /// paymobHmacSecret lets webhook tests (Payments/HandlePaymentWebhookIntegrationTests) sign
    /// requests with a secret they control, without needing real Paymob credentials — everything
    /// else about PaymobGateway besides VerifyAndParseWebhook is untouched by those tests.
    public static ServiceProvider Build(string paymobHmacSecret = "")
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IDomainEventHandler<OrderCancelledEvent>, ReleaseStockOnOrderCancelledHandler>();
        services.AddScoped<IDomainEventHandler<OrderCompletedEvent>, FulfillStockOnOrderCompletedHandler>();

        services.AddDbContext<CrowdCoffeeDbContext>(options => options.UseSqlServer(ConnectionString));

        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IOrderStockRequirementRepository, OrderStockRequirementRepository>();
        services.AddScoped<StockReservationService>();

        services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();

        services.AddMemoryCache();
        services.AddSingleton<ICatalogCache, MemoryCatalogCache>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<GetCategoriesHandler>();
        services.AddScoped<GetProductsHandler>();
        services.AddScoped<GetProductModifiersHandler>();

        services.AddScoped<RepriceCartHandler>();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderNumberGenerator, SqlOrderNumberGenerator>();
        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<AdvanceOrderStatusHandler>();
        services.AddScoped<GetKitchenBoardOrdersHandler>();

        services.AddSingleton(Options.Create(new PaymobOptions { HmacSecret = paymobHmacSecret, ApiBaseUrl = "https://accept.paymob.com" }));
        services.AddSingleton(new HttpClient { BaseAddress = new Uri("https://accept.paymob.com") });
        services.AddScoped<IPaymentGateway, PaymobGateway>();
        services.AddScoped<IPaymentEventStore, PaymentEventStore>();
        services.AddScoped<InitiatePaymentHandler>();
        services.AddScoped<HandlePaymentWebhookHandler>();
        services.AddScoped<GetReconciliationHandler>();

        return services.BuildServiceProvider();
    }
}
