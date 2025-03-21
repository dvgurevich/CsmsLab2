using System.Collections.Generic;
using System.Threading.Tasks;

public interface IOrderItemRepository
{
    Task<long> AddAsync(OrderItem orderItem);
    Task<bool> SoftDeleteAsync(long orderItemId);
    Task<IEnumerable<OrderItem>> SearchAsync(IEnumerable<long> orderIds, IEnumerable<long> productIds, bool? orderItemDeleted);
}