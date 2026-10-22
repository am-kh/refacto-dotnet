using Moq;
using Refacto.DotNet.Application.Products;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Tests.Unit
{
    /// <summary>
    /// Cas limites des handlers par type de produit, à un instant fixe en cours de journée.
    /// </summary>
    public sealed class ProductHandlersTests
    {
        private static readonly DateTime Today = new(2026, 6, 15);
        private static readonly DateTime Now = Today.AddHours(10);

        private readonly Mock<INotificationService> _notificationServiceMock = new();

        // Le jour du début de saison, en cours de journée, le produit n'est pas vendu et un délai est notifié.
        [Fact]
        public void Seasonal_JourDuDebutDeSaison_NotifieLeDelaiSansToucherAuStock()
        {
            Product product = new() { Name = "Peach", Available = 30, LeadTime = 15, SeasonStartDate = Today, SeasonEndDate = Today.AddDays(60) };

            new SeasonalProductHandler(_notificationServiceMock.Object).Process(product, Now);

            Assert.Equal(30, product.Available);
            _notificationServiceMock.Verify(n => n.SendDelayNotification(15, "Peach"), Times.Once());
            _notificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit saisonnier sans nom est notifié avec un nom vide.
        [Fact]
        public void Seasonal_ProduitSansNom_NotifieAvecUnNomVide()
        {
            Product product = new() { Name = null, Available = 0, LeadTime = 15, SeasonStartDate = Today.AddDays(-2), SeasonEndDate = Today.AddDays(60) };

            new SeasonalProductHandler(_notificationServiceMock.Object).Process(product, Now);

            _notificationServiceMock.Verify(n => n.SendDelayNotification(15, string.Empty), Times.Once());
        }

        // Un produit périssable qui expire plus tard dans la journée est encore vendable.
        [Fact]
        public void Expirable_ExpirationPlusTardAujourdhui_DecrementeLeStock()
        {
            Product product = new() { Name = "Butter", Available = 5, ExpiryDate = Today.AddHours(18) };

            new ExpirableProductHandler(_notificationServiceMock.Object).Process(product, Now);

            Assert.Equal(4, product.Available);
            _notificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit périssable sans date d'expiration lève une exception (comportement existant conservé).
        [Fact]
        public void Expirable_SansDateDExpiration_LeveUneException()
        {
            Product product = new() { Name = "Butter", Available = 5, ExpiryDate = null };

            _ = Assert.Throws<InvalidOperationException>(() => new ExpirableProductHandler(_notificationServiceMock.Object).Process(product, Now));
        }

        // Un produit normal en rupture sans nom est notifié avec un nom vide.
        [Fact]
        public void Normal_ProduitSansNomEnRupture_NotifieAvecUnNomVide()
        {
            Product product = new() { Name = null, Available = 0, LeadTime = 10 };

            new NormalProductHandler(_notificationServiceMock.Object).Process(product, Now);

            _notificationServiceMock.Verify(n => n.SendDelayNotification(10, string.Empty), Times.Once());
        }
    }
}
