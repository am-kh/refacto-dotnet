using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Application.Orders
{
    /// <summary>
    /// Port de persistance des commandes.
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Récupère une commande avec ses produits.
        /// </summary>
        /// <param name="orderId">Identifiant de la commande.</param>
        /// <returns>La commande, ou <c>null</c> si elle n'existe pas.</returns>
        Order? GetByIdWithItems(long orderId);

        /// <summary>
        /// Enregistre les modifications apportées aux commandes et à leurs produits.
        /// </summary>
        void Save();
    }
}
