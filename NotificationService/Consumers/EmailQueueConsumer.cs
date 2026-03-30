using Microsoft.AspNetCore.Connections;
using NotificationService.Services.Email;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Models;
using NotificationService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Consumers
{
    public class EmailQueueConsumer : BackgroundService
    {
        private readonly ILogger<EmailQueueConsumer> _logger;
        private readonly IEmailService _emailService;

        private IConnection? _connection;
        private IChannel? _channel;

        private readonly string _queueName = "email-queue";

        public EmailQueueConsumer(
            ILogger<EmailQueueConsumer> logger,
            IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await InitRabbitMqAsync();

            _logger.LogInformation("EmailQueueConsumer started.");

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);

                    _logger.LogInformation("Received message: {Json}", json);

                    var emailRequest = JsonSerializer.Deserialize<EmailRequest>(json);

                    if (emailRequest != null)
                    {
                        await _emailService.SendEmailAsync(emailRequest);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling RabbitMQ message");

                    await _channel.BasicNackAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _queueName,
                autoAck: false,
                consumer: consumer);

            // Keep running until shutdown
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task InitRabbitMqAsync()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("RabbitMQ connection established using IChannel.");
        }

        public override void Dispose()
        {
            _channel?.CloseAsync().Wait();
            _connection?.CloseAsync().Wait();
            base.Dispose();
        }
    }
}
