using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Data;
using NpgsqlTypes;

public class OrderHistoryRepository : IOrderHistoryRepository
{
    private readonly string _connectionString;

    public OrderHistoryRepository(DatabaseOptions dbOptions)
    {
        _connectionString = dbOptions.ConnectionString ?? throw new ArgumentNullException(nameof(dbOptions));
    }

    public async Task<long> AddAsync(OrderHistory orderHistory)
    {
        const string sql = @"
            INSERT INTO order_history (order_id, order_history_item_created_at, order_history_item_kind, order_history_item_payload)
            VALUES (@OrderId, @CreatedAt, CAST(@HistoryType AS order_history_item_kind), CAST(@Payload AS jsonb))
            RETURNING order_history_item_id;";

        var parameters = new
        {
            orderHistory.OrderId,
            CreatedAt = orderHistory.CreatedAt,
            HistoryType = orderHistory.HistoryType.ToString(),
            Payload = JsonSerializer.Serialize(orderHistory.Payload)
        };

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<long>(sql, parameters);
    }

    public async Task<IEnumerable<OrderHistory>> SearchAsync(IEnumerable<long> orderIds, IEnumerable<OrderHistoryItemKind> historyKinds)
    {
        var sql = "SELECT order_id, order_history_item_created_at, order_history_item_kind, order_history_item_payload FROM order_history WHERE 1=1";
        var parameters = new DynamicParameters();

        if (orderIds?.Any() == true)
        {
            sql += " AND order_id = ANY(@OrderIds)";
            parameters.Add("OrderIds", orderIds);
        }
        if (historyKinds?.Any() == true)
        {
            sql += " AND order_history_item_kind = ANY(@HistoryKinds)";
            parameters.Add("HistoryKinds", historyKinds);
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        var result = await connection.QueryAsync<OrderHistory>(sql, parameters);

        foreach (var history in result)
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(history.Payload.ToString());
            if (payload != null && payload.ContainsKey("EventType"))
            {
                var eventType = payload["EventType"].GetString();
                if (eventType == "ItemAddedEvent")
                {
                    var itemAddedEvent = JsonSerializer.Deserialize<ItemAddedEvent>(history.Payload.ToString());
                    history.Payload = JsonDocument.Parse(JsonSerializer.Serialize(itemAddedEvent));
                }
                else if (eventType == "OrderCreatedEvent")
                {
                    var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(history.Payload.ToString());
                    history.Payload = JsonDocument.Parse(JsonSerializer.Serialize(orderCreatedEvent));
                }
            }
        }

        return result;
    }
}