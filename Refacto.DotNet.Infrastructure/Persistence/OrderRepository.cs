using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Application.Orders;
using Refacto.DotNet.Domain.Entities;

namespace Refacto.DotNet.Infrastructure.Persistence
{
    /// <inheritdoc />
    internal sealed class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public Order? GetByIdWithItems(long orderId)
        {
            return _context.Orders
                .Include(o => o.Items)
                .SingleOrDefault(o => o.Id == orderId);
        }

        /// <inheritdoc />
        public void Save()
        {
            _ = _context.SaveChanges();
        }
    }
}
