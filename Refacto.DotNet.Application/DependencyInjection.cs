using Microsoft.Extensions.DependencyInjection;
using Refacto.DotNet.Application.Orders;
using Refacto.DotNet.Application.Products;

namespace Refacto.DotNet.Application
{
    /// <summary>
    /// Enregistrement des services de la couche Application.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Enregistre les cas d'usage et les règles de traitement par type de produit.
        /// </summary>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            _ = services.AddScoped<IOrderService, OrderService>();
            _ = services.AddScoped<IProductOrderHandler, NormalProductHandler>();
            _ = services.AddScoped<IProductOrderHandler, SeasonalProductHandler>();
            _ = services.AddScoped<IProductOrderHandler, ExpirableProductHandler>();
            return services;
        }
    }
}
