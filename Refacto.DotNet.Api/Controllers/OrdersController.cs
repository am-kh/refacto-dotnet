using Microsoft.AspNetCore.Mvc;
using Refacto.DotNet.Api.Dtos.Orders;
using Refacto.DotNet.Application.Orders;

namespace Refacto.DotNet.Api.Controllers
{
    /// <summary>
    /// Points d'entrée HTTP liés aux commandes.
    /// </summary>
    [ApiController]
    [Route("orders")]
    public sealed class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Traite la commande indiquée.
        /// </summary>
        /// <param name="orderId">Identifiant de la commande.</param>
        /// <returns>200 avec l'identifiant de la commande, ou 404 si elle n'existe pas.</returns>
        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<ProcessOrderResponse> ProcessOrder(long orderId)
        {
            if (!_orderService.ProcessOrder(orderId))
            {
                return NotFound();
            }

            return new ProcessOrderResponse(orderId);
        }
    }
}
