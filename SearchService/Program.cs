using Domain;
using SearchService.Messaging;
using SearchService.Repositories;
using StackExchange.Redis;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------
// Add Core Services
// -------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddSingleton<ISearchRepository, SearchRepository>();
builder.Services.AddScoped<ISearchService, SearchService.SearchServiceImpl>();

// -------------------------------
// Redis Configuration
// -------------------------------
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configString = builder.Configuration.GetValue<string>("Redis:Connection") ?? "localhost:6379";

    Console.WriteLine($"[REDIS] Connecting to {configString}");

    var options = ConfigurationOptions.Parse(configString);
    options.AbortOnConnectFail = false;

    var mux = ConnectionMultiplexer.Connect(options);

    Console.WriteLine($"[REDIS] Connected = {mux.IsConnected}");

    return mux;
});

// -------------------------------
// JSON Enum Serialization
// -------------------------------
builder.Services.AddControllers().AddJsonOptions(opt =>
{
    opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// -------------------------------
// RabbitMQ - Only listen to StockUpdatedEvent from WarehouseService
// -------------------------------
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

// SearchService ONLY listens to WarehouseService's StockUpdatedEvent
// This ensures proper separation of concerns:
// AddBookService -> WarehouseService -> SearchService
builder.Services.AddHostedService<StockUpdatedListener>();

// BookAddedListener REMOVED - we don't listen directly to AddBookService anymore
// builder.Services.AddHostedService<BookAddedListener>();

// -------------------------------
// Build App
// -------------------------------
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/health", () => Results.Json(new { status = "Healthy" }));

app.Run();
