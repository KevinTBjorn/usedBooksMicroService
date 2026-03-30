namespace WarehouseService.Messaging
{
    public class RabbitMqOptions
    {
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;



        // Exchanges
        public string BookAddedExchange { get; set; } = "book.events"; // fanout
        public string StockUpdatedExchange { get; set; } = "stock.events"; // fanout

        // Queues
        public string QueueName { get; set; } = "warehouse.bookadded.queue";
        public string StockUpdatedQueue { get; set; } = "search.stockupdated.queue";

        public ushort PrefetchCount { get; set; } = 10;

        // Retry configuration
        public int RetryCount { get; set; } = 10;
        public int RetryDelayMs { get; set; } = 3000;
    }
}
