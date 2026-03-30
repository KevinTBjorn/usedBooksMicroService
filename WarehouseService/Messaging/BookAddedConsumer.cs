using MassTransit;
using Domain.Events;
using Domain;

namespace WarehouseService.Messaging;

public class BookAddedConsumer : IConsumer<BookAddedEvent>
{
    private readonly IWarehouseService _warehouseService;

    public BookAddedConsumer(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    public async Task Consume(ConsumeContext<BookAddedEvent> context)
    {
        await _warehouseService.HandleBookAddedViaMassTransitAsync(context.Message);
    }
}
