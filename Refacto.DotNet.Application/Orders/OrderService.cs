using Refacto.DotNet.Application.Products;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Application.Orders
{
    /// <inheritdoc />
    internal sealed class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ProductService _productService;

        public OrderService(IOrderRepository orderRepository, ProductService productService)
        {
            _orderRepository = orderRepository;
            _productService = productService;
        }

        /// <inheritdoc />
        public bool ProcessOrder(long orderId)
        {
            Order? order = _orderRepository.GetByIdWithItems(orderId);
            if (order is null)
            {
                return false;
            }

            foreach (Product p in order.Items ?? Enumerable.Empty<Product>())
            {
                if (p.Type == "NORMAL")
                {
                    if (p.Available > 0)
                    {
                        p.Available -= 1;
                    }
                    else
                    {
                        int leadTime = p.LeadTime;
                        if (leadTime > 0)
                        {
                            _productService.NotifyDelay(leadTime, p);
                        }
                    }
                }
                else if (p.Type == "SEASONAL")
                {
                    if (DateTime.Now.Date > p.SeasonStartDate && DateTime.Now.Date < p.SeasonEndDate && p.Available > 0)
                    {
                        p.Available -= 1;
                    }
                    else
                    {
                        _productService.HandleSeasonalProduct(p);
                    }
                }
                else if (p.Type == "EXPIRABLE")
                {
                    if (p.Available > 0 && p.ExpiryDate > DateTime.Now.Date)
                    {
                        p.Available -= 1;
                    }
                    else
                    {
                        _productService.HandleExpiredProduct(p);
                    }
                }
            }

            _orderRepository.Save();
            return true;
        }
    }
}
