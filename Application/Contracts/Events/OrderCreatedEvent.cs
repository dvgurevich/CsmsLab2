public class OrderCreatedEvent
{
    public long OrderId { get; set; }
    public OrderState OrderState { get; set; }
    public string OrderCreatedBy { get; set; }

    public OrderCreatedEvent(long orderId, OrderState orderState, string orderCreatedBy)
    {
        OrderId = orderId;
        OrderState = orderState;
        OrderCreatedBy = orderCreatedBy;
    }
}