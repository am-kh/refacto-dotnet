namespace Refacto.DotNet.Controllers.Dtos.Orders
{
    /// <summary>
    /// Réponse du traitement d'une commande.
    /// </summary>
    /// <param name="id">Identifiant de la commande traitée.</param>
    public sealed record ProcessOrderResponse(long id);
}
