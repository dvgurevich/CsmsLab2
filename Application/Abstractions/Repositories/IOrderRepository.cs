using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IOrderRepository
{
    Task<long> CreateAsync(Order order);
    Task<bool> UpdateStateAsync(long orderId, OrderState newState);
    Task<IEnumerable<Order>> SearchAsync(IEnumerable<long> orderIds, OrderState? orderState, string createdBy);
    Task<Order> GetOrderByIdAsync(long orderId);
}
