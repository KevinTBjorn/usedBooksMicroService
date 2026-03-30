using System.Threading.Tasks;
using Domain.Events;

namespace WarehouseService.Messaging
{
    public interface IEventPublisher
    {
        Task PublishStockUpdatedAsync(StockUpdatedEvent evt);
        Task PublishOrderValidatedAsync(WarehouseOrderValidatedMessage msg);
        Task PublishOrderRejectedAsync(WarehouseOrderRejectedMessage msg);
    }
}
