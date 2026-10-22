using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Application.Products
{
    /// <inheritdoc />
    internal sealed class ExpirableProductHandler : IProductOrderHandler
    {
        private readonly INotificationService _notificationService;

        public ExpirableProductHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <inheritdoc />
        public string ProductType => ProductTypes.Expirable;

        /// <inheritdoc />
        public void Process(Product product, DateTime now)
        {
            if (product.HasStock && !product.IsExpired(now))
            {
                product.DecrementStock();
            }
            else
            {
                // Sans date d'expiration, une exception est levée (comportement existant conservé).
                _notificationService.SendExpirationNotification(product.NotificationName, product.ExpiryDate!.Value);
                product.ClearStock();
            }
        }
    }
}
