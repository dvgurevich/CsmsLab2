using System.Text.Json;
using System;
public class OrderHistory
{
    public long OrderId { get; set; }
    public OrderHistoryItemKind HistoryType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
}