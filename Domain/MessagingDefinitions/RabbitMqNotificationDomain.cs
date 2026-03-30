namespace Domain.MessagingDefinitions
{
    public static class RabbitMqNotificationDomain
    {
        // We reuse the same domain exchange
        public const string OrderExchange = RabbitMqOrderDomain.OrderExchange;

        // Queue where NotificationService will listen
        public const string OrderStatusChangedQueue = "notification.order-status-changed";

        // Routing key used when OrderService notifies downstream services
        public const string OrderStatusChangedKey = "order.status.changed";
    }
}
