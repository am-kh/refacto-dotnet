using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Refacto.DotNet.Application.Orders;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;
using Refacto.DotNet.Infrastructure.Persistence;

namespace Refacto.DotNet.Infrastructure
{
    /// <summary>
    /// Enregistrement des adaptateurs de la couche Infrastructure.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Enregistre la persistance (EF Core en mémoire) et le service de notification.
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            _ = services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("InMemoryDb"));
            _ = services.AddScoped<IOrderRepository, OrderRepository>();
            _ = services.AddScoped<INotificationService, NotificationService>();
            return services;
        }
    }
}
