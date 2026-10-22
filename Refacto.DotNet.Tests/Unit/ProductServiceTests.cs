using Moq;
using Refacto.DotNet.Application.Products;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Tests.Unit
{
    public sealed class ProductServiceTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _productService = new ProductService(_mockNotificationService.Object);
        }

        // Vérifie que la notification de délai est envoyée sans modifier le stock ni le délai du produit.
        [Fact]
        public void NotifyDelay_ProduitEnRupture_EnvoieLaNotificationDeDelaiSansModifierLeProduit()
        {
            // GIVEN
            Product product = new()
            {
                LeadTime = 15,
                Available = 0,
                Type = "NORMAL",
                Name = "RJ45 Cable"
            };

            // WHEN
            _productService.NotifyDelay(product.LeadTime, product);

            // THEN
            Assert.Equal(0, product.Available);
            Assert.Equal(15, product.LeadTime);
            _mockNotificationService.Verify(service => service.SendDelayNotification(product.LeadTime, product.Name), Times.Once());
        }
    }
}
