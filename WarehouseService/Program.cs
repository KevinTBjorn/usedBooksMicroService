using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using WarehouseService.Messaging;
using Domain;
using WarehouseService;
using Prometheus;
using MassTransit;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MVC controllers so API endpoints are discovered and configure JSON enum handling
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Configure RabbitMQ options from configuration
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

// Register IWarehouseService implementation
builder.Services.AddScoped<IWarehouseService, WarehouseServiceImpl>();

// Register EventListener as hosted service (consumer)
builder.Services.AddHostedService<EventListener>();

// MassTransit for RabbitMQ but only for consuming BookAddedEvent
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<BookAddedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("appuser");
            h.Password("apppassword");
        });

        cfg.ConfigureEndpoints(context);
    });
});


// Register EventPublisher as singleton (separate connection)
builder.Services.AddSingleton<IEventPublisher, EventPublisher>();

// Register DbContext
var connectionString = builder.Configuration.GetValue<string>("ConnectionStrings:WarehouseDb")
    ?? builder.Configuration.GetValue<string>("ConnectionStrings__WarehouseDb");

if (string.IsNullOrEmpty(connectionString))
{
    // Default to a Postgres connection string so EF migrations can be created using Npgsql provider.
    connectionString = "Host=postgres;Port=5432;Database=warehouse;Username=postgres;Password=postgrespw";
}

// Use Npgsql for Postgres
builder.Services.AddDbContext<WarehouseService.Data.WarehouseDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WarehouseService.Data.WarehouseDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Prometheus HTTP metrics middleware
app.UseHttpMetrics();

// Ensure controllers are mapped
app.MapControllers();

// Prometheus metrics endpoint
app.MapMetrics();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/health", () => Results.Json(new { status = "Healthy" }));

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
