public class OrderItem
{
    public long OrderItemId { get; set; }
    public long OrderId { get; set; }
    public int Quantity { get; set; }
    public long ProductId { get; set; }
    public bool? OrderItemDeleted { get; set; }
}