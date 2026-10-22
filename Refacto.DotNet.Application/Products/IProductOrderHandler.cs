using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Application.Products
{
    /// <summary>
    /// Règles de traitement d'un produit commandé, pour un type de produit donné.
    /// </summary>
    internal interface IProductOrderHandler
    {
        /// <summary>
        /// Type de produit pris en charge (voir <see cref="ProductTypes"/>).
        /// </summary>
        string ProductType { get; }

        /// <summary>
        /// Traite un produit commandé : décrémente son stock s'il est vendable,
        /// sinon met à jour sa disponibilité et notifie les clients.
        /// </summary>
        /// <param name="product">Produit commandé.</param>
        /// <param name="now">Instant du traitement de la commande.</param>
        void Process(Product product, DateTime now);
    }
}
