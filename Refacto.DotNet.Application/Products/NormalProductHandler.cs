using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Application.Products
{
    /// <inheritdoc />
    internal sealed class NormalProductHandler : IProductOrderHandler
    {
        private readonly INotificationService _notificationService;

        public NormalProductHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <inheritdoc />
        public string ProductType => ProductTypes.Normal;

        /// <inheritdoc />
        public void Process(Product product, DateTime now)
        {
            if (product.HasStock)
            {
                product.DecrementStock();
            }
            else if (product.LeadTime > 0)
            {
                _notificationService.SendDelayNotification(product.LeadTime, product.NotificationName);
            }
        }
    }
}
