using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Tests.Integration
{
    /// <summary>
    /// Règles de traitement des produits EXPIRABLE.
    /// </summary>
    [Collection("Sequential")]
    public sealed class ExpirableProductOrderTests : OrdersControllerIntegrationTestBase
    {
        public ExpirableProductOrderTests(WebApplicationFactory<Program> factory) : base(factory)
        {
        }

        // Un produit périssable non expiré et en stock est vendu : son stock diminue d'une unité, sans notification.
        [Fact]
        public async Task ProcessOrder_PerissableNonExpireEtEnStock_DecrementeLeStock()
        {
            Product result = await ProcessOrderWithSingleProduct(Expirable("Butter", available: 30, leadTime: 15, expiryDate: DateTime.Today.AddDays(26)));

            Assert.Equal(29, result.Available);
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit périssable expiré déclenche une notification d'expiration et son stock est remis à zéro.
        [Fact]
        public async Task ProcessOrder_PerissableExpire_NotifieExpirationEtMetLeStockAZero()
        {
            DateTime expiryDate = DateTime.Today.AddDays(-2);

            Product result = await ProcessOrderWithSingleProduct(Expirable("Milk", available: 6, leadTime: 90, expiryDate: expiryDate));

            Assert.Equal(0, result.Available);
            NotificationServiceMock.Verify(n => n.SendExpirationNotification("Milk", expiryDate), Times.Once());
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit périssable dont la date d'expiration est aujourd'hui est considéré comme expiré.
        [Fact]
        public async Task ProcessOrder_PerissableExpirantAujourdhui_EstConsidereCommeExpire()
        {
            DateTime expiryDate = DateTime.Today;

            Product result = await ProcessOrderWithSingleProduct(Expirable("Yogurt", available: 6, leadTime: 15, expiryDate: expiryDate));

            Assert.Equal(0, result.Available);
            NotificationServiceMock.Verify(n => n.SendExpirationNotification("Yogurt", expiryDate), Times.Once());
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit périssable en rupture mais non expiré déclenche une notification d'expiration (comportement existant conservé).
        [Fact]
        public async Task ProcessOrder_PerissableEnRuptureNonExpire_NotifieExpiration()
        {
            DateTime expiryDate = DateTime.Today.AddDays(10);

            Product result = await ProcessOrderWithSingleProduct(Expirable("Cheese", available: 0, leadTime: 15, expiryDate: expiryDate));

            Assert.Equal(0, result.Available);
            NotificationServiceMock.Verify(n => n.SendExpirationNotification("Cheese", expiryDate), Times.Once());
            NotificationServiceMock.VerifyNoOtherCalls();
        }
    }
}
