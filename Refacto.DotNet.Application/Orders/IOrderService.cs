namespace Refacto.DotNet.Application.Orders
{
    /// <summary>
    /// Cas d'usage de traitement d'une commande.
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Traite chaque produit de la commande selon les règles de son type
        /// (décrément du stock ou notification client), puis enregistre le résultat.
        /// </summary>
        /// <param name="orderId">Identifiant de la commande à traiter.</param>
        /// <returns><c>true</c> si la commande a été traitée, <c>false</c> si elle n'existe pas.</returns>
        bool ProcessOrder(long orderId);
    }
}
