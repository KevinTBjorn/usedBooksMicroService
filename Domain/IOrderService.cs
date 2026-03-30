using System.Threading.Tasks;

namespace Domain {
    public interface IOrderService {
        Task CompleteOrderAsync(OrderCompletedEvent evt);
    }
}