namespace Domain.MessagingDefinitions
{
    public static class RabbitMqOrderDomain
    {
        // Exchanges
        public const string OrderExchange = "order.exchange";

        // Validation request
        public const string OrderRequestValidationQueue = "warehouse.order.validation.queue";
        public const string OrderRequestValidationKey = "order.validation.request";

        // Validation succeeded
        public const string OrderValidatedQueue = "warehouse.order-validated";
        public const string OrderValidatedKey = "order.validated";

        // Validation failed
        public const string OrderRejectedQueue = "warehouse.order-rejected";
        public const string OrderRejectedKey = "order.rejected";


    }
}
