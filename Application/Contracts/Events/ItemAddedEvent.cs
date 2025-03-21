public class ItemAddedEvent
{
    public long OrderItemId { get; set; }
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public bool? OrderItemDeleted { get; set; }

    public ItemAddedEvent(long orderItemId, long orderId, long productId, bool? orderItemDeleted)
    {
        OrderItemId = orderItemId;
        OrderId = orderId;
        ProductId = productId;
        OrderItemDeleted = orderItemDeleted;
    }
}