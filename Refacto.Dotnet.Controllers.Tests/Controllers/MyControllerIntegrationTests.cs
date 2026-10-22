using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.Dotnet.Controllers.Tests.Controllers
{
    [Collection("Sequential")]
    public class MyControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly AppDbContext _context;
        private readonly Mock<INotificationService> _mockNotificationService;

        public MyControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _mockNotificationService = new Mock<INotificationService>();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                _ = builder.ConfigureServices(services =>
                {
                    _ = services.AddSingleton(_mockNotificationService.Object);

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
                    _ = services.AddScoped((_sp) => _mockNotificationService.Object);


                    ServiceProvider sp = services.BuildServiceProvider();
                });
            });

            IServiceScope scope = _factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        // Vérifie que le traitement d'une commande aboutit et que la commande existe toujours en base.
        [Fact]
        public async Task ProcessOrderShouldReturn()
        {
            HttpClient client = _factory.CreateClient();

            List<Product> allProducts = CreateProducts();
            HashSet<Product> orderItems = new(allProducts);
            Order order = CreateOrder(orderItems);
            await _context.Products.AddRangeAsync(allProducts);
            _ = await _context.Orders.AddAsync(order);
            _ = await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            HttpResponseMessage response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);
            _ = response.EnsureSuccessStatusCode();

            Order? resultOrder = await _context.Orders.FindAsync(order.Id);
            Assert.Equal(resultOrder.Id, order.Id);
        }

        // Vérifie que la réponse HTTP contient l'identifiant de la commande traitée.
        [Fact]
        public async Task ProcessOrder_RenvoieLIdentifiantDeLaCommande()
        {
            long orderId = await SaveOrder(Normal("USB Cable", available: 30, leadTime: 15));

            HttpResponseMessage response = await PostProcessOrder(orderId);

            _ = response.EnsureSuccessStatusCode();
            ProcessOrderResponse? body = await response.Content.ReadFromJsonAsync<ProcessOrderResponse>();
            Assert.Equal(orderId, body?.id);
        }

        // ---------- NORMAL ----------

        // Un produit normal en stock est vendu : son stock diminue d'une unité, sans notification.
        [Fact]
        public async Task ProcessOrder_NormalEnStock_DecrementeLeStock()
        {
            Product result = await ProcessOrderWithSingleProduct(Normal("USB Cable", available: 30, leadTime: 15));

            Assert.Equal(29, result.Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Un produit normal en rupture avec un délai de réapprovisionnement déclenche une notification de délai.
        [Fact]
        public async Task ProcessOrder_NormalEnRuptureAvecDelai_NotifieLeDelai()
        {
            Product result = await ProcessOrderWithSingleProduct(Normal("USB Dongle", available: 0, leadTime: 10));

            Assert.Equal(0, result.Available);
            Assert.Equal(10, result.LeadTime);
            _mockNotificationService.Verify(n => n.SendDelayNotification(10, "USB Dongle"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Un produit normal en rupture sans délai de réapprovisionnement n'entraîne aucune action.
        [Fact]
        public async Task ProcessOrder_NormalEnRuptureSansDelai_NeFaitRien()
        {
            Product result = await ProcessOrderWithSingleProduct(Normal("HDMI Cable", available: 0, leadTime: 0));

            Assert.Equal(0, result.Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // ---------- SEASONAL ----------

        // Un produit saisonnier en saison et en stock est vendu : son stock diminue d'une unité, sans notification.
        [Fact]
        public async Task ProcessOrder_SaisonnierEnSaisonEtEnStock_DecrementeLeStock()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Watermelon", available: 30, leadTime: 15, seasonStart: today.AddDays(-2), seasonEnd: today.AddDays(58));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(29, result.Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Un produit saisonnier en rupture dont le réapprovisionnement arrive avant la fin de saison déclenche une notification de délai.
        [Fact]
        public async Task ProcessOrder_SaisonnierEnRuptureDelaiDansLaSaison_NotifieLeDelai()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Watermelon", available: 0, leadTime: 15, seasonStart: today.AddDays(-2), seasonEnd: today.AddDays(58));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(0, result.Available);
            _mockNotificationService.Verify(n => n.SendDelayNotification(15, "Watermelon"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Un produit saisonnier en rupture sans délai notifie un délai de 0 jour (comportement existant conservé, contrairement au produit normal).
        [Fact]
        public async Task ProcessOrder_SaisonnierEnRuptureSansDelai_NotifieUnDelaiNul()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Watermelon", available: 0, leadTime: 0, seasonStart: today.AddDays(-2), seasonEnd: today.AddDays(58));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(0, result.Available);
            _mockNotificationService.Verify(n => n.SendDelayNotification(0, "Watermelon"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Un produit saisonnier en rupture dont le réapprovisionnement arrive après la fin de saison est notifié indisponible.
        [Fact]
        public async Task ProcessOrder_SaisonnierEnRuptureDelaiApresLaSaison_NotifieRupture()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Watermelon", available: 0, leadTime: 90, seasonStart: today.AddDays(-2), seasonEnd: today.AddDays(58));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(0, result.Available);
            _mockNotificationService.Verify(n => n.SendOutOfStockNotification("Watermelon"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Un produit saisonnier dont la saison est terminée est notifié indisponible et son stock est remis à zéro.
        [Fact]
        public async Task ProcessOrder_SaisonnierSaisonTermineeAvecStock_NotifieRuptureEtMetLeStockAZero()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Strawberry", available: 30, leadTime: 15, seasonStart: today.AddDays(-60), seasonEnd: today.AddDays(-5));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(0, result.Available);
            _mockNotificationService.Verify(n => n.SendOutOfStockNotification("Strawberry"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Un produit saisonnier dont la saison n'a pas commencé est notifié indisponible, sans toucher au stock.
        [Fact]
        public async Task ProcessOrder_SaisonnierSaisonPasCommencee_NotifieRuptureSansToucherAuStock()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Grapes", available: 30, leadTime: 15, seasonStart: today.AddDays(180), seasonEnd: today.AddDays(240));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(30, result.Available);
            _mockNotificationService.Verify(n => n.SendOutOfStockNotification("Grapes"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Un produit saisonnier pas encore en saison dont le délai dépasse la fin de saison est notifié indisponible et son stock est remis à zéro.
        [Fact]
        public async Task ProcessOrder_SaisonnierSaisonPasCommenceeDelaiApresLaSaison_NotifieRuptureEtMetLeStockAZero()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Cherry", available: 30, leadTime: 30, seasonStart: today.AddDays(10), seasonEnd: today.AddDays(20));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(0, result.Available);
            _mockNotificationService.Verify(n => n.SendOutOfStockNotification("Cherry"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Le jour du début de saison, le produit n'est pas vendu et un délai est notifié malgré le stock (comportement existant conservé).
        [Fact]
        public async Task ProcessOrder_SaisonnierLeJourDuDebutDeSaison_NotifieLeDelaiSansToucherAuStock()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Peach", available: 30, leadTime: 15, seasonStart: today, seasonEnd: today.AddDays(60));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(30, result.Available);
            _mockNotificationService.Verify(n => n.SendDelayNotification(15, "Peach"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Le dernier jour de saison, le produit n'est pas vendu : il est notifié indisponible et son stock est remis à zéro (comportement existant conservé).
        [Fact]
        public async Task ProcessOrder_SaisonnierLeDernierJourDeSaison_NotifieRuptureEtMetLeStockAZero()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Apricot", available: 30, leadTime: 15, seasonStart: today.AddDays(-10), seasonEnd: today);

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(0, result.Available);
            _mockNotificationService.Verify(n => n.SendOutOfStockNotification("Apricot"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // ---------- EXPIRABLE ----------

        // Un produit périssable non expiré et en stock est vendu : son stock diminue d'une unité, sans notification.
        [Fact]
        public async Task ProcessOrder_PerissableNonExpireEtEnStock_DecrementeLeStock()
        {
            Product result = await ProcessOrderWithSingleProduct(Expirable("Butter", available: 30, leadTime: 15, expiryDate: DateTime.Today.AddDays(26)));

            Assert.Equal(29, result.Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Un produit périssable expiré déclenche une notification d'expiration et son stock est remis à zéro.
        [Fact]
        public async Task ProcessOrder_PerissableExpire_NotifieExpirationEtMetLeStockAZero()
        {
            DateTime expiryDate = DateTime.Today.AddDays(-2);

            Product result = await ProcessOrderWithSingleProduct(Expirable("Milk", available: 6, leadTime: 90, expiryDate: expiryDate));

            Assert.Equal(0, result.Available);
            _mockNotificationService.Verify(n => n.SendExpirationNotification("Milk", expiryDate), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Un produit périssable dont la date d'expiration est aujourd'hui est considéré comme expiré.
        [Fact]
        public async Task ProcessOrder_PerissableExpirantAujourdhui_EstConsidereCommeExpire()
        {
            DateTime expiryDate = DateTime.Today;

            Product result = await ProcessOrderWithSingleProduct(Expirable("Yogurt", available: 6, leadTime: 15, expiryDate: expiryDate));

            Assert.Equal(0, result.Available);
            _mockNotificationService.Verify(n => n.SendExpirationNotification("Yogurt", expiryDate), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Un produit périssable en rupture mais non expiré déclenche une notification d'expiration (comportement existant conservé).
        [Fact]
        public async Task ProcessOrder_PerissableEnRuptureNonExpire_NotifieExpiration()
        {
            DateTime expiryDate = DateTime.Today.AddDays(10);

            Product result = await ProcessOrderWithSingleProduct(Expirable("Cheese", available: 0, leadTime: 15, expiryDate: expiryDate));

            Assert.Equal(0, result.Available);
            _mockNotificationService.Verify(n => n.SendExpirationNotification("Cheese", expiryDate), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // ---------- AUTRES ----------

        // Un produit de type inconnu est ignoré : ni modification de stock, ni notification.
        [Fact]
        public async Task ProcessOrder_TypeInconnu_EstIgnore()
        {
            Product product = new() { LeadTime = 15, Available = 30, Type = "UNKNOWN", Name = "Mystery Box" };

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(30, result.Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        // Une commande de plusieurs produits traite chacun d'eux selon les règles de son type.
        [Fact]
        public async Task ProcessOrder_CommandeMultiProduits_TraiteChaqueProduitSelonSonType()
        {
            DateTime today = DateTime.Today;
            DateTime milkExpiryDate = today.AddDays(-2);
            long orderId = await SaveOrder(
                Normal("USB Cable", available: 30, leadTime: 15),
                Normal("USB Dongle", available: 0, leadTime: 10),
                Expirable("Butter", available: 30, leadTime: 15, expiryDate: today.AddDays(26)),
                Expirable("Milk", available: 6, leadTime: 90, expiryDate: milkExpiryDate),
                Seasonal("Watermelon", available: 30, leadTime: 15, seasonStart: today.AddDays(-2), seasonEnd: today.AddDays(58)),
                Seasonal("Grapes", available: 30, leadTime: 15, seasonStart: today.AddDays(180), seasonEnd: today.AddDays(240)));

            HttpResponseMessage response = await PostProcessOrder(orderId);

            _ = response.EnsureSuccessStatusCode();
            Dictionary<string, int> availableByName = await _context.Products.AsNoTracking().ToDictionaryAsync(p => p.Name!, p => p.Available);
            Assert.Equal(29, availableByName["USB Cable"]);
            Assert.Equal(0, availableByName["USB Dongle"]);
            Assert.Equal(29, availableByName["Butter"]);
            Assert.Equal(0, availableByName["Milk"]);
            Assert.Equal(29, availableByName["Watermelon"]);
            Assert.Equal(30, availableByName["Grapes"]);
            _mockNotificationService.Verify(n => n.SendDelayNotification(10, "USB Dongle"), Times.Once());
            _mockNotificationService.Verify(n => n.SendExpirationNotification("Milk", milkExpiryDate), Times.Once());
            _mockNotificationService.Verify(n => n.SendOutOfStockNotification("Grapes"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Persiste une commande contenant les produits donnés et renvoie son identifiant.
        /// </summary>
        private async Task<long> SaveOrder(params Product[] products)
        {
            Order order = CreateOrder(new HashSet<Product>(products));
            await _context.Products.AddRangeAsync(products);
            _ = await _context.Orders.AddAsync(order);
            _ = await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
            return order.Id;
        }

        /// <summary>
        /// Appelle l'endpoint de traitement de la commande donnée.
        /// </summary>
        private Task<HttpResponseMessage> PostProcessOrder(long orderId)
        {
            return _factory.CreateClient().PostAsync($"/orders/{orderId}/processOrder", null);
        }

        /// <summary>
        /// Traite une commande contenant un seul produit et renvoie l'état du produit relu depuis la base.
        /// </summary>
        private async Task<Product> ProcessOrderWithSingleProduct(Product product)
        {
            long orderId = await SaveOrder(product);

            HttpResponseMessage response = await PostProcessOrder(orderId);
            _ = response.EnsureSuccessStatusCode();

            return await _context.Products.AsNoTracking().SingleAsync(p => p.Id == product.Id);
        }

        private static Product Normal(string name, int available, int leadTime)
        {
            return new Product { Type = "NORMAL", Name = name, Available = available, LeadTime = leadTime };
        }

        private static Product Seasonal(string name, int available, int leadTime, DateTime seasonStart, DateTime seasonEnd)
        {
            return new Product { Type = "SEASONAL", Name = name, Available = available, LeadTime = leadTime, SeasonStartDate = seasonStart, SeasonEndDate = seasonEnd };
        }

        private static Product Expirable(string name, int available, int leadTime, DateTime expiryDate)
        {
            return new Product { Type = "EXPIRABLE", Name = name, Available = available, LeadTime = leadTime, ExpiryDate = expiryDate };
        }

        private static Order CreateOrder(HashSet<Product> products)
        {
            return new Order { Items = products };
        }

        private static List<Product> CreateProducts()
        {
            return new List<Product>
            {
                new Product { LeadTime = 15, Available = 30, Type = "NORMAL", Name = "USB Cable" },
                new Product { LeadTime = 10, Available = 0, Type = "NORMAL", Name = "USB Dongle" },
                new Product { LeadTime = 15, Available = 30, Type = "EXPIRABLE", Name = "Butter", ExpiryDate = DateTime.Now.AddDays(26) },
                new Product { LeadTime = 90, Available = 6, Type = "EXPIRABLE", Name = "Milk", ExpiryDate = DateTime.Now.AddDays(-2) },
                new Product { LeadTime = 15, Available = 30, Type = "SEASONAL", Name = "Watermelon", SeasonStartDate = DateTime.Now.AddDays(-2), SeasonEndDate = DateTime.Now.AddDays(58) },
                new Product { LeadTime = 15, Available = 30, Type = "SEASONAL", Name = "Grapes", SeasonStartDate = DateTime.Now.AddDays(180), SeasonEndDate = DateTime.Now.AddDays(240) }
            };
        }
    }
}
