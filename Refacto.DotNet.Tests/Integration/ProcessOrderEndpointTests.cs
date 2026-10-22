using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Refacto.DotNet.Api.Dtos.Orders;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Tests.Integration
{
    /// <summary>
    /// Tests du contrat HTTP de l'endpoint et du traitement d'une commande dans son ensemble.
    /// </summary>
    [Collection("Sequential")]
    public sealed class ProcessOrderEndpointTests : OrdersControllerIntegrationTestBase
    {
        public ProcessOrderEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
        {
        }

        // Vérifie que le traitement d'une commande aboutit et que la commande existe toujours en base.
        [Fact]
        public async Task ProcessOrder_CommandeExistante_RenvoieOk()
        {
            HttpClient client = Factory.CreateClient();

            List<Product> allProducts = CreateProducts();
            HashSet<Product> orderItems = new(allProducts);
            Order order = CreateOrder(orderItems);
            await Context.Products.AddRangeAsync(allProducts);
            _ = await Context.Orders.AddAsync(order);
            _ = await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();

            HttpResponseMessage response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);
            _ = response.EnsureSuccessStatusCode();

            Order? resultOrder = await Context.Orders.FindAsync(order.Id);
            Assert.NotNull(resultOrder);
            Assert.Equal(order.Id, resultOrder.Id);
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

        // Vérifie qu'une commande inexistante renvoie 404 sans aucune notification.
        [Fact]
        public async Task ProcessOrder_CommandeIntrouvable_RenvoieNotFound()
        {
            HttpResponseMessage response = await PostProcessOrder(orderId: 999);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit de type inconnu est ignoré : ni modification de stock, ni notification.
        [Fact]
        public async Task ProcessOrder_TypeInconnu_EstIgnore()
        {
            Product product = new() { LeadTime = 15, Available = 30, Type = "UNKNOWN", Name = "Mystery Box" };

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(30, result.Available);
            NotificationServiceMock.VerifyNoOtherCalls();
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
            Dictionary<string, int> availableByName = await Context.Products.AsNoTracking().ToDictionaryAsync(p => p.Name!, p => p.Available);
            Assert.Equal(29, availableByName["USB Cable"]);
            Assert.Equal(0, availableByName["USB Dongle"]);
            Assert.Equal(29, availableByName["Butter"]);
            Assert.Equal(0, availableByName["Milk"]);
            Assert.Equal(29, availableByName["Watermelon"]);
            Assert.Equal(30, availableByName["Grapes"]);
            NotificationServiceMock.Verify(n => n.SendDelayNotification(10, "USB Dongle"), Times.Once());
            NotificationServiceMock.Verify(n => n.SendExpirationNotification("Milk", milkExpiryDate), Times.Once());
            NotificationServiceMock.Verify(n => n.SendOutOfStockNotification("Grapes"), Times.Once());
            NotificationServiceMock.VerifyNoOtherCalls();
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
