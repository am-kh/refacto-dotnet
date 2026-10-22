namespace Refacto.DotNet.Domain.Entities
{
    /// <summary>
    /// Produit du catalogue.
    /// </summary>
    public sealed class Product
    {
        /// <summary>
        /// Identifiant du produit.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Nombre de jours nécessaires au réapprovisionnement.
        /// </summary>
        public int LeadTime { get; set; }

        /// <summary>
        /// Nombre d'unités disponibles en stock.
        /// </summary>
        public int Available { get; set; }

        /// <summary>
        /// Type de produit : NORMAL, SEASONAL ou EXPIRABLE.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Nom du produit, utilisé dans les notifications client.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Date d'expiration (produits EXPIRABLE).
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Début de la saison de disponibilité (produits SEASONAL).
        /// </summary>
        public DateTime? SeasonStartDate { get; set; }

        /// <summary>
        /// Fin de la saison de disponibilité (produits SEASONAL).
        /// </summary>
        public DateTime? SeasonEndDate { get; set; }
    }
}
