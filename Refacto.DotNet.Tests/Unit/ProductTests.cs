using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Tests.Unit
{
    /// <summary>
    /// Règles de disponibilité portées par l'entité <see cref="Product"/>, testées à un instant fixe.
    /// </summary>
    public sealed class ProductTests
    {
        private static readonly DateTime Today = new(2026, 6, 15);
        private static readonly DateTime Now = Today.AddHours(10);

        // Le jour du début de saison est exclu, même en cours de journée.
        [Fact]
        public void IsInSeason_JourDuDebutDeSaisonEnCoursDeJournee_RenvoieFalse()
        {
            Product product = new() { SeasonStartDate = Today, SeasonEndDate = Today.AddDays(30) };

            Assert.False(product.IsInSeason(Now));
        }

        // Le lendemain du début de saison, le produit est en saison.
        [Fact]
        public void IsInSeason_LendemainDuDebutDeSaison_RenvoieTrue()
        {
            Product product = new() { SeasonStartDate = Today.AddDays(-1), SeasonEndDate = Today.AddDays(30) };

            Assert.True(product.IsInSeason(Now));
        }

        // Le jour de fin de saison est exclu.
        [Fact]
        public void IsInSeason_JourDeFinDeSaison_RenvoieFalse()
        {
            Product product = new() { SeasonStartDate = Today.AddDays(-30), SeasonEndDate = Today };

            Assert.False(product.IsInSeason(Now));
        }

        // Une saison commencée plus tôt dans la journée n'est plus « à venir » : la comparaison se fait à l'heure près.
        [Fact]
        public void IsBeforeSeason_SaisonCommenceePlusTotDansLaJournee_RenvoieFalse()
        {
            Product product = new() { SeasonStartDate = Today.AddHours(8) };

            Assert.False(product.IsBeforeSeason(Now));
        }

        // Une saison qui commence plus tard dans la journée est encore à venir.
        [Fact]
        public void IsBeforeSeason_SaisonCommencantPlusTardDansLaJournee_RenvoieTrue()
        {
            Product product = new() { SeasonStartDate = Today.AddHours(12) };

            Assert.True(product.IsBeforeSeason(Now));
        }

        // Un réapprovisionnement arrivant quelques heures après la fin de saison arrive trop tard : la comparaison se fait à l'heure près.
        [Fact]
        public void IsRestockedAfterSeasonEnd_ArriveeQuelquesHeuresApresLaFin_RenvoieTrue()
        {
            Product product = new() { LeadTime = 15, SeasonEndDate = Today.AddDays(15).AddHours(8) };

            Assert.True(product.IsRestockedAfterSeasonEnd(Now));
        }

        // Un réapprovisionnement arrivant exactement à la fin de saison n'est pas considéré comme tardif.
        [Fact]
        public void IsRestockedAfterSeasonEnd_ArriveeExactementALaFin_RenvoieFalse()
        {
            Product product = new() { LeadTime = 15, SeasonEndDate = Now.AddDays(15) };

            Assert.False(product.IsRestockedAfterSeasonEnd(Now));
        }

        // Une expiration aujourd'hui avant l'heure courante n'est pas encore atteinte : seule la date du jour est comparée.
        [Fact]
        public void IsExpired_ExpirationAujourdhuiAvantLHeureCourante_RenvoieFalse()
        {
            Product product = new() { ExpiryDate = Today.AddHours(8) };

            Assert.False(product.IsExpired(Now));
        }

        // Un produit dont la date d'expiration est aujourd'hui à minuit est expiré.
        [Fact]
        public void IsExpired_ExpirationAujourdhuiAMinuit_RenvoieTrue()
        {
            Product product = new() { ExpiryDate = Today };

            Assert.True(product.IsExpired(Now));
        }

        // Un produit sans date d'expiration est considéré comme expiré.
        [Fact]
        public void IsExpired_SansDateDExpiration_RenvoieTrue()
        {
            Product product = new() { ExpiryDate = null };

            Assert.True(product.IsExpired(Now));
        }
    }
}
