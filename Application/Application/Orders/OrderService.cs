using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IOrderHistoryRepository _orderHistoryRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IOrderItemRepository orderItemRepository,
        IOrderHistoryRepository orderHistoryRepository,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _orderHistoryRepository = orderHistoryRepository;
        _logger = logger;
    }

    public async Task<long> CreateOrderAsync(Order order)
    {
        var orderId = await _orderRepository.CreateAsync(order);
        await _orderHistoryRepository.AddAsync(new OrderHistory
        {
            OrderId = orderId,
            HistoryType = OrderHistoryItemKind.created
        });
        return orderId;
    }

    public async Task<bool> AddOrderItemAsync(long orderId, OrderItem orderItem)
    {
        var orderExists = await _orderRepository.SearchAsync(new List<long> { orderId }, OrderState.created, null);
        if (orderExists == null || !orderExists.Any())
            throw new InvalidOperationException("Items can only be added to orders in 'created' status.");
        
        var itemId = await _orderItemRepository.AddAsync(orderItem);
        await _orderHistoryRepository.AddAsync(new OrderHistory
        {
            OrderId = orderId,
            HistoryType = OrderHistoryItemKind.item_added
        });
        return itemId > 0;
    }

    public async Task<bool> RemoveOrderItemAsync(long orderItemId)
    {
        var itemDeleted = await _orderItemRepository.SoftDeleteAsync(orderItemId);
        if (itemDeleted)
        {
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                OrderId = orderItemId,
                HistoryType = OrderHistoryItemKind.item_removed
            });
        }
        return itemDeleted;
    }

    public async Task<bool> ChangeOrderStatusAsync(long orderId, OrderState newStatus)
    {   
        var order = await _orderRepository.GetOrderByIdAsync(orderId);
        if (order == null) return false;
        Console.WriteLine(order.OrderId);

        if (newStatus == OrderState.processing && order.OrderState != OrderState.created)
            throw new InvalidOperationException("Only orders in 'created' status can be processed.");
        if (newStatus == OrderState.completed && order.OrderState != OrderState.processing)
            throw new InvalidOperationException("Only orders in 'processing' status can be completed.");
   
        var updated = await _orderRepository.UpdateStateAsync(orderId, newStatus);
        if (updated)
        {
            order.OrderState = newStatus;
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                OrderId = orderId,
                HistoryType = OrderHistoryItemKind.state_changed
            });
        }
        return updated;
    }

    public async Task<IEnumerable<OrderHistory>> GetOrderHistoryAsync(long orderId)
    {
        return await _orderHistoryRepository.SearchAsync(new List<long> { orderId }, null);
    }
}