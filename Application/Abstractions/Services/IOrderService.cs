using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IOrderService
{
    Task<long> CreateOrderAsync(Order order);
    Task<bool> AddOrderItemAsync(long orderId, OrderItem orderItem);
    Task<bool> RemoveOrderItemAsync(long orderItemId);
    Task<bool> ChangeOrderStatusAsync(long orderId, OrderState newStatus);
    Task<IEnumerable<OrderHistory>> GetOrderHistoryAsync(long orderId);
}