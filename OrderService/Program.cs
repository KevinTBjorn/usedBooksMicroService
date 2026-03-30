using Microsoft.EntityFrameworkCore;
using OrderService.Consumers;
using OrderService.Data;
using OrderService.Extensions;
using OrderService.Messaging;
using Prometheus;
using RabbitMQ.Client;
using System.Text.Json;

namespace OrderService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddDbContext<OrderDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("OrdersDb")));

            builder.Services.AddSingleton<IConnectionFactory>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var rabbitSection = config.GetSection("RabbitMq");

                return new ConnectionFactory
                {
                    HostName = rabbitSection["Host"],
                    Port = int.Parse(rabbitSection["Port"]),
                    UserName = rabbitSection["Username"],
                    Password = rabbitSection["Password"],
                };
            });

            builder.Services.AddSingleton(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

            builder.Services.AddHostedService<WarehouseOrderValidatedConsumer>();
            builder.Services.AddHostedService<WarehouseOrderRejectedConsumer>();

            // Add services to the container.

            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();


            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();


            app.UseHttpMetrics();
            app.MapMetrics("/metrics");

            app.UseSwagger();
            app.UseSwaggerUI();

            app.ApplyMigrations();

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();
            app.MapGet("/", () => "Order service running!");

            app.Run();
        }
    }
}
