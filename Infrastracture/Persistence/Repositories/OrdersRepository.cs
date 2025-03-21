using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Options;
using System.Text.Json;

public class OrderRepository : IOrderRepository
{
    private readonly string _connectionString;
    private readonly IOrderHistoryRepository _orderHistoryRepository;

    public OrderRepository(DatabaseOptions dbOptions, IOrderHistoryRepository orderHistoryRepository)
    {
        _connectionString = dbOptions.ConnectionString ?? throw new ArgumentNullException(nameof(dbOptions));
        _orderHistoryRepository = orderHistoryRepository ?? throw new ArgumentNullException(nameof(orderHistoryRepository));
    }

    public async Task<long> CreateAsync(Order order)
    {
        const string sql = @"
            INSERT INTO orders (order_state, order_created_by, order_created_at)
            VALUES (@OrderState::order_state, @OrderCreatedBy, @OrderCreatedAt)
            RETURNING order_id;";

        await using var connection = new NpgsqlConnection(_connectionString);
        var parameters = new
        {
            OrderState = order.OrderState.ToString(),
            OrderCreatedBy = order.OrderCreatedBy,
            OrderCreatedAt = order.OrderCreatedAt
        };

        long orderId = await connection.ExecuteScalarAsync<long>(sql, parameters);

        var orderCreatedEvent = new OrderCreatedEvent(orderId, order.OrderState, order.OrderCreatedBy);
        
        await _orderHistoryRepository.AddAsync(new OrderHistory
        {
            OrderId = orderId,
            HistoryType = OrderHistoryItemKind.created,
            CreatedAt = DateTime.UtcNow,
            Payload = JsonDocument.Parse(JsonSerializer.Serialize(orderCreatedEvent))
        });

        return orderId;
    }

    public async Task<bool> UpdateStateAsync(long orderId, OrderState newState)
    {
        const string sql = @"
            UPDATE orders 
            SET order_state = @NewState::order_state 
            WHERE order_id = @OrderId;";

        var parameters = new
        {
            OrderId = orderId,
            NewState = newState.ToString()
        };

        await using var connection = new NpgsqlConnection(_connectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, parameters);
        
        if (rowsAffected > 0)
        {
            var orderStateUpdatedEvent = new OrderStateUpdatedEvent(orderId, newState);
            
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                OrderId = orderId,
                HistoryType = OrderHistoryItemKind.state_changed,
                CreatedAt = DateTime.UtcNow,
                Payload = JsonDocument.Parse(JsonSerializer.Serialize(orderStateUpdatedEvent))
            });
            
            return true;
        }

        return false;
    }

    public async Task<IEnumerable<Order>> SearchAsync(IEnumerable<long> orderIds, OrderState? orderState, string orderCreatedBy)
    {
        var sql = "SELECT order_id, order_state, order_created_by, order_created_at FROM orders WHERE 1=1";
        var parameters = new DynamicParameters();

        if (orderIds?.Any() == true)
        {
            sql += " AND order_id = ANY(@OrderIds)";
            parameters.Add("OrderIds", orderIds);
        }
        if (orderState.HasValue)
        {
            sql += " AND order_state = @OrderState::order_state";
            parameters.Add("OrderState", orderState.ToString());
        }
        if (!string.IsNullOrEmpty(orderCreatedBy))
        {
            sql += " AND order_created_by ILIKE @OrderCreatedBy";
            parameters.Add("OrderCreatedBy", $"%{orderCreatedBy}%");
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<Order>(sql, parameters);
    }
    
    public async Task<Order> GetOrderByIdAsync(long orderId)
    {
        const string sql = @"
            SELECT order_id, order_state, order_created_by 
            FROM orders
            WHERE order_id = @OrderId;";

        var parameters = new { OrderId = orderId };

        await using var connection = new NpgsqlConnection(_connectionString);
        var order = await connection.QueryAsync<Order>(sql, parameters);
        Console.WriteLine (order.OrderId);

        return order;
    }
}