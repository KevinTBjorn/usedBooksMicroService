using System.Threading.Tasks;
using Domain.Events;
using System.Collections.Generic;
using System;
using Domain.Dto;

namespace Domain {
    public interface IWarehouseService {
        Task HandleBookAddedAsync(BookAddedEvent evt);
        Task HandleOrderCompletedAsync(OrderCompletedEvent evt);
        Task<List<BookWithStockDto>> GetAllBooksAsync();
        Task<BookInventoryDto?> GetInventoryForBookAsync(Guid bookId);
        Task<OrderValidationResult> ValidateOrderAsync(OrderValidationRequestMessage request);
        Task HandleBookAddedViaMassTransitAsync(BookAddedEvent message);
        Task<List<Book>> GetBooksAsync(int pageNumber, int pageSize);
        Task<List<UserBookListingDto>> GetUserBooksByBookIdAsync(Guid bookId);
        Task<List<UserBookListingDto>> GetUserBooksByUserIdAsync(Guid userId);
    }

    public class OrderCompletedEvent {
        public string OrderId { get; set; }
        public List<OrderItem> Items { get; set; }
    }

    public class OrderItem {
        public string BookId { get; set; }
        public int Quantity { get; set; }
    }
}