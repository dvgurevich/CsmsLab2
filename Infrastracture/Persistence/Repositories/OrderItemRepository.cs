using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Options;
using System.Text.Json;

public class OrderItemRepository : IOrderItemRepository
{
    private readonly string _connectionString;
    private readonly IOrderHistoryRepository _orderHistoryRepository;

    public OrderItemRepository(DatabaseOptions dbOptions, IOrderHistoryRepository orderHistoryRepository)
    {
        _connectionString = dbOptions.ConnectionString ?? throw new ArgumentNullException(nameof(dbOptions));
        _orderHistoryRepository = orderHistoryRepository ?? throw new ArgumentNullException(nameof(orderHistoryRepository));
    }

public async Task<long> AddAsync(OrderItem orderItem)
{
    // Проверяем, существует ли продукт с указанным ID
    const string checkProductSql = "SELECT COUNT(1) FROM products WHERE product_id = @ProductId";
    
    await using var connection = new NpgsqlConnection(_connectionString);
    
    var productExists = await connection.ExecuteScalarAsync<bool>(checkProductSql, new { orderItem.ProductId });
    
    if (!productExists)
    {
        // Добавляем продукт, если он не существует
        const string insertProductSql = @"
            INSERT INTO products (product_id, name, price)
            VALUES (@ProductId, @ProductName, @ProductPrice);";
        
        var productInsertParameters = new
        {
            ProductId = orderItem.ProductId,
            ProductName = "New Product",  // Здесь можно добавить реальные данные
            ProductPrice = 100.0         // Укажите цену или другие атрибуты продукта
        };

        await connection.ExecuteAsync(insertProductSql, productInsertParameters);
    }

    // Теперь вставляем товар в таблицу order_items
    const string sql = @"
        INSERT INTO order_items (order_id, product_id, order_item_deleted, order_item_quantity)
        VALUES (@OrderId, @ProductId, @OrderItemDeleted, @OrderItemQuantity)
        RETURNING order_item_id;";

    var parameters = new
    {
        orderItem.OrderId,
        orderItem.ProductId,
        orderItem.OrderItemDeleted,
        OrderItemQuantity = orderItem.Quantity
    };

    var orderItemId = await connection.ExecuteScalarAsync<long>(sql, parameters);

    var itemAddedEvent = new ItemAddedEvent(
        orderItemId,
        orderItem.OrderId,
        orderItem.ProductId,
        orderItem.OrderItemDeleted
    );

    await _orderHistoryRepository.AddAsync(new OrderHistory
    {
        OrderId = orderItem.OrderId,
        HistoryType = OrderHistoryItemKind.item_added,
        CreatedAt = DateTime.UtcNow,
        Payload = JsonDocument.Parse(JsonSerializer.Serialize(itemAddedEvent))
    });

    return orderItemId;
}

    public async Task<bool> SoftDeleteAsync(long orderItemId)
    {
        const string sql = @"
            UPDATE order_items 
            SET order_item_deleted = true 
            WHERE order_item_id = @OrderItemId;";

        var parameters = new { OrderItemId = orderItemId };

        await using var connection = new NpgsqlConnection(_connectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, parameters);

        if (rowsAffected > 0)
        {
            const string selectSql = "SELECT order_id, product_id, order_item_deleted FROM order_items WHERE order_item_id = @OrderItemId";
            var orderItem = await connection.QuerySingleOrDefaultAsync<OrderItem>(selectSql, new { OrderItemId = orderItemId });

            if (orderItem != null)
            {
                var itemSoftDeletedEvent = new ItemSoftDeletedEvent(
                    orderItemId,
                    orderItem.OrderId,
                    orderItem.ProductId,
                    orderItem.OrderItemDeleted
                );

                await _orderHistoryRepository.AddAsync(new OrderHistory
                {
                    OrderId = orderItem.OrderId,
                    HistoryType = OrderHistoryItemKind.item_removed,
                    CreatedAt = DateTime.UtcNow,
                    Payload = JsonDocument.Parse(JsonSerializer.Serialize(itemSoftDeletedEvent))
                });
            }
        }

        return rowsAffected > 0;
    }

    public async Task<IEnumerable<OrderItem>> SearchAsync(IEnumerable<long> orderIds, IEnumerable<long> productIds, bool? orderItemDeleted)
    {
        var sql = "SELECT order_item_id, order_id, product_id, order_item_deleted FROM order_items WHERE 1=1";
        var parameters = new DynamicParameters();

        if (orderIds?.Any() == true)
        {
            sql += " AND order_id = ANY(@OrderIds)";
            parameters.Add("OrderIds", orderIds);
        }

        if (productIds?.Any() == true)
        {
            sql += " AND product_id = ANY(@ProductIds)";
            parameters.Add("ProductIds", productIds);
        }
        if (orderItemDeleted.HasValue)
        {
            sql += " AND order_item_deleted = @OrderItemDeleted";
            parameters.Add("OrderItemDeleted", orderItemDeleted.Value);
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<OrderItem>(sql, parameters);
    }
}