using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Tests.Integration
{
    /// <summary>
    /// Règles de traitement des produits NORMAL.
    /// </summary>
    [Collection("Sequential")]
    public sealed class NormalProductOrderTests : OrdersControllerIntegrationTestBase
    {
        public NormalProductOrderTests(WebApplicationFactory<Program> factory) : base(factory)
        {
        }

        // Un produit normal en stock est vendu : son stock diminue d'une unité, sans notification.
        [Fact]
        public async Task ProcessOrder_NormalEnStock_DecrementeLeStock()
        {
            Product result = await ProcessOrderWithSingleProduct(Normal("USB Cable", available: 30, leadTime: 15));

            Assert.Equal(29, result.Available);
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit normal en rupture avec un délai de réapprovisionnement déclenche une notification de délai.
        [Fact]
        public async Task ProcessOrder_NormalEnRuptureAvecDelai_NotifieLeDelai()
        {
            Product result = await ProcessOrderWithSingleProduct(Normal("USB Dongle", available: 0, leadTime: 10));

            Assert.Equal(0, result.Available);
            Assert.Equal(10, result.LeadTime);
            NotificationServiceMock.Verify(n => n.SendDelayNotification(10, "USB Dongle"), Times.Once());
            NotificationServiceMock.VerifyNoOtherCalls();
        }

        // Un produit normal en rupture sans délai de réapprovisionnement n'entraîne aucune action.
        [Fact]
        public async Task ProcessOrder_NormalEnRuptureSansDelai_NeFaitRien()
        {
            Product result = await ProcessOrderWithSingleProduct(Normal("HDMI Cable", available: 0, leadTime: 0));

            Assert.Equal(0, result.Available);
            NotificationServiceMock.VerifyNoOtherCalls();
        }
    }
}
