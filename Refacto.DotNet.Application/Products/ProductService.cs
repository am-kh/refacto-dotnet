using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Application.Products
{
    /// <summary>
    /// Traitements des produits non vendables (rupture, hors saison, expiration).
    /// La persistance est assurée par l'appelant.
    /// </summary>
    internal sealed class ProductService
    {
        private readonly INotificationService _notificationService;

        public ProductService(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Notifie le client du délai de réapprovisionnement du produit.
        /// </summary>
        public void NotifyDelay(int leadTime, Product p)
        {
            p.LeadTime = leadTime;
            _notificationService.SendDelayNotification(leadTime, p.Name);
        }

        /// <summary>
        /// Traite un produit saisonnier non vendable : indisponible si hors saison
        /// ou si le réapprovisionnement dépasse la fin de saison, délai notifié sinon.
        /// </summary>
        public void HandleSeasonalProduct(Product p)
        {
            if (DateTime.Now.AddDays(p.LeadTime) > p.SeasonEndDate)
            {
                _notificationService.SendOutOfStockNotification(p.Name);
                p.Available = 0;
            }
            else if (p.SeasonStartDate > DateTime.Now)
            {
                _notificationService.SendOutOfStockNotification(p.Name);
            }
            else
            {
                NotifyDelay(p.LeadTime, p);
            }
        }

        /// <summary>
        /// Traite un produit périssable non vendable : notifie l'expiration et vide le stock.
        /// </summary>
        public void HandleExpiredProduct(Product p)
        {
            if (p.Available > 0 && p.ExpiryDate > DateTime.Now)
            {
                p.Available -= 1;
            }
            else
            {
                _notificationService.SendExpirationNotification(p.Name, (DateTime)p.ExpiryDate);
                p.Available = 0;
            }
        }
    }
}
