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

        /// <summary>
        /// Indique s'il reste au moins une unité en stock.
        /// </summary>
        public bool HasStock => Available > 0;

        /// <summary>
        /// Retire une unité du stock.
        /// </summary>
        public void DecrementStock()
        {
            Available -= 1;
        }

        /// <summary>
        /// Vide le stock : le produit devient indisponible.
        /// </summary>
        public void ClearStock()
        {
            Available = 0;
        }

        /// <summary>
        /// Indique si le jour de l'instant donné est strictement compris entre le début et la fin de saison
        /// (les jours de début et de fin sont exclus).
        /// </summary>
        public bool IsInSeason(DateTime now)
        {
            return now.Date > SeasonStartDate && now.Date < SeasonEndDate;
        }

        /// <summary>
        /// Indique si la saison n'a pas encore commencé à l'instant donné.
        /// </summary>
        public bool IsBeforeSeason(DateTime now)
        {
            return SeasonStartDate > now;
        }

        /// <summary>
        /// Indique si un réapprovisionnement lancé à l'instant donné arriverait après la fin de saison.
        /// </summary>
        public bool IsRestockedAfterSeasonEnd(DateTime now)
        {
            return now.AddDays(LeadTime) > SeasonEndDate;
        }

        /// <summary>
        /// Indique si le produit est expiré au jour de l'instant donné (date d'expiration atteinte ou absente).
        /// </summary>
        public bool IsExpired(DateTime now)
        {
            return !(ExpiryDate > now.Date);
        }
    }
}
