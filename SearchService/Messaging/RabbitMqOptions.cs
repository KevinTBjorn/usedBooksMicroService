namespace SearchService.Messaging
{
    public class RabbitMqOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;

        public string UserName { get; set; } = "guest"; 
        public string Password { get; set; } = "guest";

        public string BookAddedExchange { get; set; } = "book.events";
        public string StockUpdatedExchange { get; set; } = "stock.events";

        // queue names used by search service
        public string BookAddedQueue { get; set; } = "search.bookadded.queue";
        public string StockUpdatedQueue { get; set; } = "search.stockupdated.queue";

        public ushort PrefetchCount { get; set; } = 10;
    }
}
