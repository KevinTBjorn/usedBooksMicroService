using AddBookService.Services;
using Domain;
using MassTransit;
using Prometheus;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using HealthChecks.RabbitMQ;



var builder = WebApplication.CreateBuilder(args);

// Custom Prometheus gauge for RabbitMQ health
var rabbitmqHealthGauge = Prometheus.Metrics.CreateGauge(
    "rabbitmq_health_status",
    "RabbitMQ connection health (1 = healthy, 0 = unhealthy)"
);

// Health checks: self + async RabbitMQ check
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddAsyncCheck("rabbitmq", async () =>
    {
        try
        {
            var factory = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = new Uri("amqp://appuser:apppassword@rabbitmq:5672/"),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            };
            using var connection = await factory.CreateConnectionAsync();
            rabbitmqHealthGauge.Set(1); // Healthy
            return HealthCheckResult.Healthy("RabbitMQ connection successful");
        }
        catch (Exception ex)
        {
            rabbitmqHealthGauge.Set(0); // Unhealthy
            return HealthCheckResult.Unhealthy("RabbitMQ connection failed", ex);
        }
    })
    .ForwardToPrometheus();

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("appuser");
            h.Password("apppassword");
        });

        cfg.UseMessageRetry(r =>
        {
            r.Interval(10, TimeSpan.FromSeconds(5));
        });
    });
});


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IAddBookService, AddBookService.Services.AddBookService>();

// Configure HttpClient for AddBookService with timeout
builder.Services.AddHttpClient<IAddBookService, AddBookService.Services.AddBookService>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

// Configure HttpClient for GenreDetectionService with timeout
builder.Services.AddHttpClient<IGenreService, GenreDetectionService>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });


var app = builder.Build();


// Swagger
app.UseSwagger();
app.UseSwaggerUI();

if (!builder.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
//app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

// Prometheus middleware (HTTP metrics)
app.UseHttpMetrics();

// Controllers
app.MapControllers();


// PROMETHEUS METRICS ENDPOINT
app.MapMetrics();

// Health endpoints
static async Task WriteHealthJson(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var json = JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description
        })
    }, new JsonSerializerOptions { WriteIndented = true });

    await context.Response.WriteAsync(json);
}

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = reg => reg.Name == "self",
    ResponseWriter = WriteHealthJson
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    // Expose all checks except the self liveness check
    Predicate = reg => reg.Name != "self",
    ResponseWriter = WriteHealthJson
});

app.Run();