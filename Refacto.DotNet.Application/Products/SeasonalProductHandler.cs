using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Application.Products
{
    /// <inheritdoc />
    internal sealed class SeasonalProductHandler : IProductOrderHandler
    {
        private readonly INotificationService _notificationService;

        public SeasonalProductHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <inheritdoc />
        public string ProductType => ProductTypes.Seasonal;

        /// <inheritdoc />
        public void Process(Product product, DateTime now)
        {
            if (product.IsInSeason(now) && product.HasStock)
            {
                product.DecrementStock();
            }
            else if (product.IsRestockedAfterSeasonEnd(now))
            {
                _notificationService.SendOutOfStockNotification(product.NotificationName);
                product.ClearStock();
            }
            else if (product.IsBeforeSeason(now))
            {
                _notificationService.SendOutOfStockNotification(product.NotificationName);
            }
            else
            {
                _notificationService.SendDelayNotification(product.LeadTime, product.NotificationName);
            }
        }
    }
}
