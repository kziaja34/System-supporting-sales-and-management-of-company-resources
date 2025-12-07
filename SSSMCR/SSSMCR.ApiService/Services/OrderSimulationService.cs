using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services

{
    
    
    public class NoProductsAvailableException : Exception
    {
        public NoProductsAvailableException(string message) : base(message) { }
    }

    public class OrderSimulationException : Exception
    {
        public OrderSimulationException(string message, Exception inner = null!) : base(message, inner) { }
    }

    public interface IOrderSimulationService
    {
        Task<OrderSimulationResult> SimulateOrderAsync(
            int minProducts = 1, int maxProducts = 3,
            int minQty = 1, int maxQty = 5);
    }

    public class OrderSimulationService(AppDbContext db, ILogger<OrderSimulationService> logger)
        : IOrderSimulationService
    {
        private readonly Random _rnd = new Random();
        
        private readonly string[] _firstNames = new[] { "Anna", "Jan", "Piotr", "Maria", "Katarzyna", "Marek", "Ewa", "Tomasz" };
        private readonly string[] _lastNames = new[] { "Kowalski", "Nowak", "Wiśniewski", "Wójcik", "Kamiński", "Lewandowski" };
        private readonly string[] _streets = new[] { "Polna", "Kwiatowa", "Leśna", "Szkolna", "Ogrodowa", "Miodowa" };
        private readonly string[] _cities = new[] { "Warszawa", "Kraków", "Wrocław", "Gdańsk", "Poznań" };
        private readonly string[] _countries = new[] { "Polska" };

        public async Task<OrderSimulationResult> SimulateOrderAsync(int minProducts = 1, int maxProducts = 3, int minQty = 1, int maxQty = 5)
        {
            if (minProducts <= 0 || maxProducts < minProducts) throw new ArgumentException("Nieprawidłowe wartości min/max products.");
            if (minQty <= 0 || maxQty < minQty) throw new ArgumentException("Nieprawidłowe wartości min/max qty.");

            try
            {
                var products = await db.Products
                    .ToListAsync();

                if (products == null || products.Count == 0)
                    throw new NoProductsAvailableException("Brak dostępnych produktów do symulacji.");
                
                var firstName = RandomFrom(_firstNames);
                var lastName = RandomFrom(_lastNames);
                var email = $"{firstName.ToLower()}.{lastName.ToLower()}{_rnd.Next(10, 999)}@example.com";

                var address = new
                {
                    Street = $"{RandomFrom(_streets)} {_rnd.Next(1, 200)}",
                    City = RandomFrom(_cities),
                    PostalCode = $"{_rnd.Next(10, 99)}-{_rnd.Next(100, 999)}",
                    Country = RandomFrom(_countries)
                };
                
                var productsCount = _rnd.Next(minProducts, maxProducts + 1);
                var chosen = products.OrderBy(_ => _rnd.Next()).Take(productsCount).ToList();
                
                var order = new Order
                {
                    CustomerName = $"{firstName} {lastName}",
                    CustomerEmail = email,
                    ShippingAddress = $"{address.PostalCode } { address.City } { address.Street}",
                    CreatedAt = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    Items = new List<OrderItem>()
                };

                foreach (var p in chosen)
                {
                    var qty = _rnd.Next(minQty, maxQty + 1);
                    var unitPrice = p.UnitPrice;
                    
                    var item = new OrderItem
                    {
                        ProductId = p.Id,
                        Quantity = qty,
                        UnitPrice = unitPrice,
                        Order = null,
                        Product = null
                    };
                    order.Items.Add(item);
                }
                
                await using var tx = await db.Database.BeginTransactionAsync();
                try
                {
                    db.Orders.Add(order);
                    await db.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    logger.LogError(ex, "Błąd zapisu symulowanego zamówienia do bazy.");
                    throw new OrderSimulationException("Nie udało się zapisać symulowanego zamówienia.", ex);
                }

                logger.LogInformation("Utworzono symulowane zamówienie Id={OrderId}", order.Id);

                return new OrderSimulationResult
                {
                    OrderId = order.Id,
                    ItemsCount = order.Items.Count,
                    CreatedAt = order.CreatedAt
                };
            }
            catch (NoProductsAvailableException)
            {
                throw;
            }
            catch (OrderSimulationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Niespodziewany błąd w OrderSimulationService.");
                throw new OrderSimulationException("Wystąpił błąd podczas symulacji zamówienia.", ex);
            }
        }

        private string RandomFrom(string[] arr) => arr[_rnd.Next(arr.Length)];
    }
}