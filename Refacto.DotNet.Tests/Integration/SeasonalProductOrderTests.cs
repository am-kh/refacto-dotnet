using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Tests.Integration
{
    /// <summary>
    /// Règles de traitement des produits SEASONAL.
    /// </summary>
    [Collection("Sequential")]
    public sealed class SeasonalProductOrderTests : OrdersControllerIntegrationTestBase
    {
        public SeasonalProductOrderTests(WebApplicationFactory<Program> factory) : base(factory)
        {
        }

        // Un produit saisonnier en saison et en stock est vendu : son stock diminue d'une unité, sans notification.
        [Fact]
        public async Task ProcessOrder_SaisonnierEnSaisonEtEnStock_DecrementeLeStock()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Watermelon", available: 30, leadTime: 15, seasonStart: today.AddDays(-2), seasonEnd: today.AddDays(58));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(29, result.Available);
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit saisonnier en rupture dont le réapprovisionnement arrive avant la fin de saison déclenche une notification de délai.
        [Fact]
        public async Task ProcessOrder_SaisonnierEnRuptureDelaiDansLaSaison_NotifieLeDelai()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Watermelon", available: 0, leadTime: 15, seasonStart: today.AddDays(-2), seasonEnd: today.AddDays(58));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(0, result.Available);
            NotificationServiceMock.Verify(n => n.SendDelayNotification(15, "Watermelon"), Times.Once());
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit saisonnier en rupture sans délai notifie un délai de 0 jour (comportement existant conservé, contrairement au produit normal).
        [Fact]
        public async Task ProcessOrder_SaisonnierEnRuptureSansDelai_NotifieUnDelaiNul()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Watermelon", available: 0, leadTime: 0, seasonStart: today.AddDays(-2), seasonEnd: today.AddDays(58));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(0, result.Available);
            NotificationServiceMock.Verify(n => n.SendDelayNotification(0, "Watermelon"), Times.Once());
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit saisonnier en rupture dont le réapprovisionnement arrive après la fin de saison est notifié indisponible.
        [Fact]
        public async Task ProcessOrder_SaisonnierEnRuptureDelaiApresLaSaison_NotifieRupture()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Watermelon", available: 0, leadTime: 90, seasonStart: today.AddDays(-2), seasonEnd: today.AddDays(58));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(0, result.Available);
            NotificationServiceMock.Verify(n => n.SendOutOfStockNotification("Watermelon"), Times.Once());
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit saisonnier dont la saison est terminée est notifié indisponible et son stock est remis à zéro.
        [Fact]
        public async Task ProcessOrder_SaisonnierSaisonTermineeAvecStock_NotifieRuptureEtMetLeStockAZero()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Strawberry", available: 30, leadTime: 15, seasonStart: today.AddDays(-60), seasonEnd: today.AddDays(-5));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(0, result.Available);
            NotificationServiceMock.Verify(n => n.SendOutOfStockNotification("Strawberry"), Times.Once());
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit saisonnier dont la saison n'a pas commencé est notifié indisponible, sans toucher au stock.
        [Fact]
        public async Task ProcessOrder_SaisonnierSaisonPasCommencee_NotifieRuptureSansToucherAuStock()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Grapes", available: 30, leadTime: 15, seasonStart: today.AddDays(180), seasonEnd: today.AddDays(240));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(30, result.Available);
            NotificationServiceMock.Verify(n => n.SendOutOfStockNotification("Grapes"), Times.Once());
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit saisonnier pas encore en saison dont le délai dépasse la fin de saison est notifié indisponible et son stock est remis à zéro.
        [Fact]
        public async Task ProcessOrder_SaisonnierSaisonPasCommenceeDelaiApresLaSaison_NotifieRuptureEtMetLeStockAZero()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Cherry", available: 30, leadTime: 30, seasonStart: today.AddDays(10), seasonEnd: today.AddDays(20));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(0, result.Available);
            NotificationServiceMock.Verify(n => n.SendOutOfStockNotification("Cherry"), Times.Once());
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Le jour du début de saison, le produit n'est pas vendu et un délai est notifié malgré le stock (comportement existant conservé).
        [Fact]
        public async Task ProcessOrder_SaisonnierLeJourDuDebutDeSaison_NotifieLeDelaiSansToucherAuStock()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Peach", available: 30, leadTime: 15, seasonStart: today, seasonEnd: today.AddDays(60));

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(30, result.Available);
            NotificationServiceMock.Verify(n => n.SendDelayNotification(15, "Peach"), Times.Once());
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Le dernier jour de saison, le produit n'est pas vendu : il est notifié indisponible et son stock est remis à zéro (comportement existant conservé).
        [Fact]
        public async Task ProcessOrder_SaisonnierLeDernierJourDeSaison_NotifieRuptureEtMetLeStockAZero()
        {
            DateTime today = DateTime.Today;
            Product product = Seasonal("Apricot", available: 30, leadTime: 15, seasonStart: today.AddDays(-10), seasonEnd: today);

            Product result = await ProcessOrderWithSingleProduct(product);

            Assert.Equal(0, result.Available);
            NotificationServiceMock.Verify(n => n.SendOutOfStockNotification("Apricot"), Times.Once());
            NotificationServiceMock.VerifyNoOtherCalls();
        }
    }
}
