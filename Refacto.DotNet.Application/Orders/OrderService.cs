using Refacto.DotNet.Application.Products;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Application.Orders
{
    /// <inheritdoc />
    internal sealed class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IReadOnlyDictionary<string, IProductOrderHandler> _handlersByProductType;
        private readonly TimeProvider _timeProvider;

        public OrderService(IOrderRepository orderRepository, IEnumerable<IProductOrderHandler> handlers, TimeProvider timeProvider)
        {
            _orderRepository = orderRepository;
            _handlersByProductType = handlers.ToDictionary(h => h.ProductType);
            _timeProvider = timeProvider;
        }

        /// <inheritdoc />
        public bool ProcessOrder(long orderId)
        {
            Order? order = _orderRepository.GetByIdWithItems(orderId);
            if (order is null)
            {
                return false;
            }

            // Un seul instant de référence pour tous les produits de la commande.
            DateTime now = _timeProvider.GetLocalNow().DateTime;

            foreach (Product product in order.Items ?? Enumerable.Empty<Product>())
            {
                // Les produits d'un type inconnu sont ignorés.
                if (product.Type is not null && _handlersByProductType.TryGetValue(product.Type, out IProductOrderHandler? handler))
                {
                    handler.Process(product, now);
                }
            }

            _orderRepository.Save();
            return true;
        }
    }
}
