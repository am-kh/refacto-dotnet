namespace Refacto.DotNet.Domain.Entities
{
    /// <summary>
    /// Commande client.
    /// </summary>
    public sealed class Order
    {
        /// <summary>
        /// Identifiant de la commande.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Produits commandés.
        /// </summary>
        public ICollection<Product>? Items { get; set; }
    }
}
