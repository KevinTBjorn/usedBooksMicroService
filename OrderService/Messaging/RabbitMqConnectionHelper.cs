using RabbitMQ.Client;

namespace OrderService.Messaging;

public static class RabbitMqConnectionHelper
{
    public static IConnection CreateConnectionWithRetry(IConnectionFactory factory)
    {
        int maxRetries = 10;
        int delayMs = 2000;

        for (int i = 1; i <= maxRetries; i++)
        {
            try
            {
                return factory.CreateConnection();
            }
            catch
            {
                Console.WriteLine($"RabbitMQ not ready, retry {i}/{maxRetries}...");
                Thread.Sleep(delayMs);
            }
        }

        throw new Exception("RabbitMQ could not be reached after retries.");
    }
}
