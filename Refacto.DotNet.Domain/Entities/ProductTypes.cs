namespace Refacto.DotNet.Domain.Entities
{
    /// <summary>
    /// Types de produit connus (valeurs de <see cref="Product.Type"/>).
    /// </summary>
    public static class ProductTypes
    {
        /// <summary>
        /// Produit sans particularité.
        /// </summary>
        public const string Normal = "NORMAL";

        /// <summary>
        /// Produit disponible uniquement pendant sa saison.
        /// </summary>
        public const string Seasonal = "SEASONAL";

        /// <summary>
        /// Produit vendable jusqu'à sa date d'expiration.
        /// </summary>
        public const string Expirable = "EXPIRABLE";
    }
}
