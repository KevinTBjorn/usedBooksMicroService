using Domain;
using Domain.Dto;
using Domain.Events;
using Microsoft.EntityFrameworkCore;
using WarehouseService.Models;

namespace WarehouseService
{
    public class WarehouseServiceImpl : IWarehouseService
    {
        private readonly Data.WarehouseDbContext _db;
        private readonly Messaging.IEventPublisher _publisher;

        public WarehouseServiceImpl(Data.WarehouseDbContext db, Messaging.IEventPublisher publisher)
        {
            _db = db;
            _publisher = publisher;
        }


        public async Task HandleBookAddedAsync(BookAddedEvent evt)
        {
            // create Book if not exists
            var existingBook = await _db.Books.FindAsync(evt.BookId);
            if (existingBook == null)
            {
                existingBook = new WarehouseService.Models.Book(
                    evt.BookId, 
                    evt.Title, 
                    evt.ISBN, 
                    evt.Description, 
                    evt.Edition, 
                    evt.Year, 
                    evt.Author, 
                    evt.ImageUrl, 
                    evt.Genre);
                _db.Books.Add(existingBook);
            }

            // create or update UserBook based on evt.UserId
            var userBook = await _db.UserBooks.FindAsync(evt.BookId, evt.UserId);
            if (userBook == null)
            {
                _db.UserBooks.Add(new UserBook(evt.BookId, evt.UserId, evt.Condition ?? "New", evt.InitialStock, evt.Price));
            }
            else
            {
                // set Quantity via EF property access
                var entry = _db.Entry(userBook);
                entry.Property("Quantity").CurrentValue = evt.InitialStock;
                entry.Property("Condition").CurrentValue = evt.Condition ?? "New";
                entry.Property("Price").CurrentValue = evt.Price;
            }

            await _db.SaveChangesAsync();

            // compute total quantity and emit StockUpdatedEvent with full metadata
            var totalQty = await _db.UserBooks.Where(ub => ub.BookId == evt.BookId).SumAsync(ub => ub.Quantity);

            var stockEvt = new StockUpdatedEvent 
            { 
                BookId = existingBook.Id,
                Stock = totalQty,
                OccurredAt = DateTime.UtcNow,
                
                // Include book metadata for SearchService
                Title = existingBook.Title,
                ISBN = existingBook.ISBN,
                Description = existingBook.Description,
                Edition = existingBook.Edition,
                Year = existingBook.Year,
                Author = existingBook.Author,
                ImageUrl = existingBook.ImageUrl,
                Genre = existingBook.Genre
            };
            
            await _publisher.PublishStockUpdatedAsync(stockEvt);
            
            Console.WriteLine($"WAREHOUSE_PUBLISHED StockUpdatedEvent BookId={existingBook.Id} Stock={totalQty}");
        }

        public async Task HandleBookAddedViaMassTransitAsync(BookAddedEvent evt)
        {
            var existingBook = await _db.Books
                               .SingleOrDefaultAsync(b => b.ISBN == evt.ISBN);

            if (existingBook == null)
            {
                // Create new book
                existingBook = new WarehouseService.Models.Book(
                    evt.BookId,
                    evt.Title,
                    evt.ISBN,
                    evt.Description,
                    evt.Edition,
                    evt.Year,
                    evt.Author,
                    evt.ImageUrl,
                    evt.Genre
                );

                _db.Books.Add(existingBook);
                await _db.SaveChangesAsync();
            }

            // Create or update UserBook
            var userBook = await _db.UserBooks.FindAsync(existingBook.Id, evt.UserId);

            if (userBook == null)
            {
                userBook = new UserBook(
                    existingBook.Id,
                    evt.UserId,
                    evt.Condition ?? "New",
                    evt.InitialStock,
                    evt.Price
                );

                _db.UserBooks.Add(userBook);
            }
            else
            {
                userBook.Quantity += evt.InitialStock;
            }

            await _db.SaveChangesAsync();

            // IMPORTANT: Calculate total stock and emit StockUpdatedEvent with full metadata
            var totalQty = await _db.UserBooks
                .Where(ub => ub.BookId == existingBook.Id)
                .SumAsync(ub => ub.Quantity);

            var stockEvt = new StockUpdatedEvent 
            { 
                BookId = existingBook.Id, 
                Stock = totalQty,
                OccurredAt = DateTime.UtcNow,
                
                // Include book metadata for SearchService
                Title = existingBook.Title,
                ISBN = existingBook.ISBN,
                Description = existingBook.Description,
                Edition = existingBook.Edition,
                Year = existingBook.Year,
                Author = existingBook.Author,
                ImageUrl = existingBook.ImageUrl,
                Genre = existingBook.Genre
            };

            await _publisher.PublishStockUpdatedAsync(stockEvt);

            Console.WriteLine($"WAREHOUSE_PUBLISHED StockUpdatedEvent BookId={existingBook.Id} Stock={totalQty}");
        }


        public async Task HandleOrderCompletedAsync(OrderCompletedEvent evt)
        {
            // reduce stock across userbooks
            foreach (var it in evt.Items)
            {
                var needed = it.Quantity;
                var items = await _db.UserBooks.Where(i => i.BookId == Guid.Parse(it.BookId) && i.Quantity > 0)
                    .OrderByDescending(i => i.Quantity)
                    .ToListAsync();

                foreach (var ub in items)
                {
                    if (needed <= 0) break;
                    var take = Math.Min(ub.Quantity, needed);
                    var entry = _db.Entry(ub);
                    entry.Property("Quantity").CurrentValue = (int)ub.Quantity - take;
                    needed -= take;
                }
            }

            await _db.SaveChangesAsync();

            // Emit StockUpdatedEvent for each affected book with full metadata
            var bookIds = evt.Items.Select(i => Guid.Parse(i.BookId)).Distinct();
            foreach (var bookId in bookIds)
            {
                var book = await _db.Books.FindAsync(bookId);
                if (book == null) continue;

                var total = await _db.UserBooks.Where(i => i.BookId == bookId).SumAsync(i => i.Quantity);
                
                var stockEvt = new StockUpdatedEvent 
                { 
                    BookId = bookId, 
                    Stock = total,
                    OccurredAt = DateTime.UtcNow,
                    
                    // Include book metadata
                    Title = book.Title,
                    ISBN = book.ISBN,
                    Description = book.Description,
                    Edition = book.Edition,
                    Year = book.Year,
                    Author = book.Author,
                    ImageUrl = book.ImageUrl,
                    Genre = book.Genre
                };
                
                await _publisher.PublishStockUpdatedAsync(stockEvt);
                
                Console.WriteLine($"WAREHOUSE_PUBLISHED StockUpdatedEvent (after order) BookId={bookId} Stock={total}");
            }
        }

        public async Task<List<Domain.Dto.BookWithStockDto>> GetAllBooksAsync()
        {
            var books = await _db.Books
                .Select(b => new Domain.Dto.BookWithStockDto
                {
                    BookId = b.Id,
                    Title = b.Title,
                    ISBN = b.ISBN,
                    Description = b.Description,
                    Edition = b.Edition,
                    Year = b.Year,
                    Author = b.Author,
                    ImageUrl = b.ImageUrl,
                    Genre = b.Genre,
                    TotalStock = _db.UserBooks.Where(ub => ub.BookId == b.Id).Sum(ub => (int?)ub.Quantity) ?? 0
                })
                .ToListAsync();

            return books;
        }

        public async Task<Domain.Dto.BookInventoryDto?> GetInventoryForBookAsync(Guid bookId)
        {
            var book = await _db.Books.FindAsync(bookId);
            if (book == null) return null;

            var listings = await _db.UserBooks
                .Where(ub => ub.BookId == bookId)
                .Select(ub => new Domain.Dto.UserBookListingDto
                {
                    BookId = ub.BookId,
                    UserId = ub.UserId,
                    Condition = ub.Condition,
                    Quantity = ub.Quantity,
                    Price = ub.Price
                })
                .ToListAsync();

            var total = listings.Sum(l => l.Quantity);

            var dto = new Domain.Dto.BookInventoryDto
            {
                BookId = book.Id,
                TotalQuantity = total,
                Listings = listings
            };

            return dto;
        }

        public async Task<Domain.Events.OrderValidationResult> ValidateOrderAsync(OrderValidationRequestMessage request)
        {
            var reasons = new List<string>();

            foreach (var item in request.Items)
            {
                var userBook = await _db.UserBooks
                    .FirstOrDefaultAsync(x => x.UserId == request.UserId && x.BookId == item.BookId);

                if (userBook == null)
                {
                    reasons.Add($"BookId: {item.BookId} not found in user inventory");
                    continue;
                }

                if (item.Quantity <= 0)
                {
                    reasons.Add($"Invalid quantity for BookId: {item.BookId}");
                    continue;
                }

                if (userBook.Quantity < item.Quantity)
                {
                    reasons.Add($"BookId: {item.BookId} out of stock (have {userBook.Quantity}, need {item.Quantity})");
                }
            }

            if (reasons.Count == 0)
                return new Domain.Events.OrderValidationResult { IsValid = true };

            return new Domain.Events.OrderValidationResult { IsValid = false, Reasons = reasons };
        }

        public async Task<List<Domain.Book>> GetBooksAsync(int pageNumber, int pageSize)
        {
            var booksQuery = await _db.Books
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .Select(b => new Domain.Book(
                    b.Id,
                    b.Title,
                    b.ISBN,
                    b.Description,
                    b.Edition,
                    b.Year,
                    b.Author,
                    b.ImageUrl,
                    b.Genre
                ))
                .ToListAsync();

            return booksQuery;
        }

        // Get all UserBooks by BookId/ISBN { store/bookID }
        public async Task<List<UserBookListingDto>> GetUserBooksByBookIdAsync(Guid bookId)
        {
            return await _db.UserBooks
                .Where(ub => ub.BookId == bookId)
                .Select(ub => new UserBookListingDto
                {
                    BookId = ub.BookId,
                    UserId = ub.UserId,
                    Condition = ub.Condition,
                    Quantity = ub.Quantity,
                    Price = ub.Price
                })
                .ToListAsync();
        }
        // Should return all UserBookListingDto for a given userId, we may need to later join with Books to get more info
        public Task<List<UserBookListingDto>> GetUserBooksByUserIdAsync(Guid userId)
        {
            return _db.UserBooks
                .Where(ub => ub.UserId == userId)
                .Select(ub => new UserBookListingDto
                {
                    BookId = ub.BookId,
                    UserId = ub.UserId,
                    Condition = ub.Condition,
                    Quantity = ub.Quantity,
                    Price = ub.Price
                })
                .ToListAsync();

        }
    }
}