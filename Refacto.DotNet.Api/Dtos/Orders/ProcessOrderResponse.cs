namespace Refacto.DotNet.Api.Dtos.Orders
{
    /// <summary>
    /// Réponse du traitement d'une commande.
    /// </summary>
    /// <param name="Id">Identifiant de la commande traitée.</param>
    public sealed record ProcessOrderResponse(long Id);
}
