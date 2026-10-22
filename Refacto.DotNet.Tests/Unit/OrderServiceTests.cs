using Microsoft.Extensions.Time.Testing;
using Moq;
using Refacto.DotNet.Application.Orders;
using Refacto.DotNet.Application.Products;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Tests.Unit
{
    /// <summary>
    /// Orchestration du traitement d'une commande : sélection du handler, horloge et persistance.
    /// </summary>
    public sealed class OrderServiceTests
    {
        private static readonly DateTimeOffset Now = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);

        private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
        private readonly Mock<IProductOrderHandler> _normalHandlerMock = new();
        private readonly Mock<IProductOrderHandler> _seasonalHandlerMock = new();
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _ = _normalHandlerMock.Setup(h => h.ProductType).Returns(ProductTypes.Normal);
            _ = _seasonalHandlerMock.Setup(h => h.ProductType).Returns(ProductTypes.Seasonal);

            FakeTimeProvider timeProvider = new(Now);
            timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);

            _orderService = new OrderService(_orderRepositoryMock.Object, new[] { _normalHandlerMock.Object, _seasonalHandlerMock.Object }, timeProvider);
        }

        // Une commande introuvable n'est pas traitée et rien n'est enregistré.
        [Fact]
        public void ProcessOrder_CommandeIntrouvable_RenvoieFalseSansEnregistrer()
        {
            _ = _orderRepositoryMock.Setup(r => r.GetByIdWithItems(42)).Returns((Order?)null);

            bool processed = _orderService.ProcessOrder(42);

            Assert.False(processed);
            _orderRepositoryMock.Verify(r => r.Save(), Times.Never());
        }

        // Chaque produit est confié au handler de son type, avec l'instant fourni par l'horloge, puis la commande est enregistrée une fois.
        [Fact]
        public void ProcessOrder_CommandeExistante_DelegueAuHandlerDuTypeEtEnregistreUneFois()
        {
            Product normal = new() { Type = ProductTypes.Normal };
            Product seasonal = new() { Type = ProductTypes.Seasonal };
            _ = _orderRepositoryMock.Setup(r => r.GetByIdWithItems(1)).Returns(new Order { Id = 1, Items = new List<Product> { normal, seasonal } });

            bool processed = _orderService.ProcessOrder(1);

            Assert.True(processed);
            _normalHandlerMock.Verify(h => h.Process(normal, Now.DateTime), Times.Once());
            _normalHandlerMock.Verify(h => h.Process(seasonal, It.IsAny<DateTime>()), Times.Never());
            _seasonalHandlerMock.Verify(h => h.Process(seasonal, Now.DateTime), Times.Once());
            _orderRepositoryMock.Verify(r => r.Save(), Times.Once());
        }

        // Un produit sans type ou de type inconnu est ignoré sans erreur.
        [Theory]
        [InlineData(null)]
        [InlineData("UNKNOWN")]
        public void ProcessOrder_TypeAbsentOuInconnu_EstIgnore(string? type)
        {
            Product product = new() { Type = type };
            _ = _orderRepositoryMock.Setup(r => r.GetByIdWithItems(1)).Returns(new Order { Id = 1, Items = new List<Product> { product } });

            bool processed = _orderService.ProcessOrder(1);

            Assert.True(processed);
            _normalHandlerMock.Verify(h => h.Process(It.IsAny<Product>(), It.IsAny<DateTime>()), Times.Never());
            _seasonalHandlerMock.Verify(h => h.Process(It.IsAny<Product>(), It.IsAny<DateTime>()), Times.Never());
            _orderRepositoryMock.Verify(r => r.Save(), Times.Once());
        }
    }
}
