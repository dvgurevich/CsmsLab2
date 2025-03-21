public class OrderStateUpdatedEvent
{
    public long OrderId { get; set; }
    public OrderState NewState { get; set; }

    public OrderStateUpdatedEvent(long orderId, OrderState newState)
    {
        OrderId = orderId;
        NewState = newState;
    }
}