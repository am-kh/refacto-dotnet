using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Domain.Entities;
using Refacto.DotNet.Infrastructure.Persistence;

namespace Refacto.DotNet.Tests.Integration
{
    /// <summary>
    /// Socle des tests d'intégration de l'endpoint de traitement de commande :
    /// API hébergée en mémoire, base EF InMemory réinitialisée à chaque test et service de notification simulé.
    /// </summary>
    public abstract class OrdersControllerIntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
    {
        protected OrdersControllerIntegrationTestBase(WebApplicationFactory<Program> factory)
        {
            NotificationServiceMock = new Mock<INotificationService>();

            Factory = factory.WithWebHostBuilder(builder =>
            {
                _ = builder.ConfigureServices(services =>
                {
                    _ = services.AddSingleton(NotificationServiceMock.Object);

                    ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        _ = services.Remove(descriptor);
                    }

                    // Add ApplicationDbContext using an in-memory database for testing
                    _ = services.AddDbContext<AppDbContext>(options =>
                    {
                        _ = options.UseInMemoryDatabase($"InMemoryDbForTesting-{GetType()}");
                    });
                    _ = services.AddScoped((_sp) => NotificationServiceMock.Object);
                });
            });

            IServiceScope scope = Factory.Services.CreateScope();
            Context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Context.Database.EnsureDeleted();
            Context.Database.EnsureCreated();
        }

        protected WebApplicationFactory<Program> Factory { get; }

        private protected AppDbContext Context { get; }

        protected Mock<INotificationService> NotificationServiceMock { get; }

        /// <summary>
        /// Persiste une commande contenant les produits donnés et renvoie son identifiant.
        /// </summary>
        protected async Task<long> SaveOrder(params Product[] products)
        {
            Order order = CreateOrder(new HashSet<Product>(products));
            await Context.Products.AddRangeAsync(products);
            _ = await Context.Orders.AddAsync(order);
            _ = await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return order.Id;
        }

        /// <summary>
        /// Appelle l'endpoint de traitement de la commande donnée.
        /// </summary>
        protected Task<HttpResponseMessage> PostProcessOrder(long orderId)
        {
            return Factory.CreateClient().PostAsync($"/orders/{orderId}/processOrder", null);
        }

        /// <summary>
        /// Traite une commande contenant un seul produit et renvoie l'état du produit relu depuis la base.
        /// </summary>
        protected async Task<Product> ProcessOrderWithSingleProduct(Product product)
        {
            long orderId = await SaveOrder(product);

            HttpResponseMessage response = await PostProcessOrder(orderId);
            _ = response.EnsureSuccessStatusCode();

            return await Context.Products.AsNoTracking().SingleAsync(p => p.Id == product.Id);
        }

        protected static Product Normal(string name, int available, int leadTime)
        {
            return new Product { Type = "NORMAL", Name = name, Available = available, LeadTime = leadTime };
        }

        protected static Product Seasonal(string name, int available, int leadTime, DateTime seasonStart, DateTime seasonEnd)
        {
            return new Product { Type = "SEASONAL", Name = name, Available = available, LeadTime = leadTime, SeasonStartDate = seasonStart, SeasonEndDate = seasonEnd };
        }

        protected static Product Expirable(string name, int available, int leadTime, DateTime expiryDate)
        {
            return new Product { Type = "EXPIRABLE", Name = name, Available = available, LeadTime = leadTime, ExpiryDate = expiryDate };
        }

        protected static Order CreateOrder(HashSet<Product> products)
        {
            return new Order { Items = products };
        }
    }
}
