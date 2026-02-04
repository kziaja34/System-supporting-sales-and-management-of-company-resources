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

    public class OrderSimulationService(AppDbContext db, ILogger<OrderSimulationService> logger, IProductService productService)
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
                var products = await db.Products.ToListAsync();
                if (products == null || products.Count == 0)
                    throw new NoProductsAvailableException("Brak dostępnych produktów do symulacji.");

                // optionally pick a random branch like seeder
                var branches = await db.Branches.ToListAsync();
                var branch = branches.Any() ? branches[_rnd.Next(branches.Count)] : null;

                var firstName = RandomFrom(_firstNames);
                var lastName = RandomFrom(_lastNames);
                var email = $"{firstName.ToLower()}.{lastName.ToLower()}{_rnd.Next(10, 999)}@example.com";

                var address = new
                {
                    Street = $"{RandomFrom(_streets)} {_rnd.Next(1, 200)}",
                    City = "Będzin",
                    PostalCode = "42-500",
                    Country = "Polska"
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
                    Items = new List<OrderItem>(),
                    Branch = branch,
                    
                    Shipping = new Shipping
                    {
                        TargetPoint = "BED05M",
                        IsLabelGenerated = false,
                        ExternalShipmentId = null,
                        TrackingNumber = null
                    }
                };

                foreach (var p in chosen)
                {
                    var qty = _rnd.Next(minQty, maxQty + 1);
                    var unitPrice = p.UnitPrice;

                    var item = new OrderItem
                    {
                        ProductId = p.Id,
                        Quantity = qty,
                        UnitPrice = unitPrice
                        // NIE ustawiamy Order ani Product - EF ustawi relation po dodaniu do order.Items
                    };
                    order.Items.Add(item);
                }

                // Ustaw agregaty tak jak robi to seeder
                order.ItemsCount = order.Items.Count;
                order.TotalPrice = order.Items.Sum(i => i.UnitPrice * i.Quantity);

                await using var tx = await db.Database.BeginTransactionAsync();
                try
                {
                    db.Orders.Add(order);
                    // opcjonalnie zarejestruj itemy explicite (bezpieczne):
                    db.OrderItems.AddRange(order.Items);

                    await db.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    logger.LogError(ex, "Błąd zapisu symulowanego zamówienia do bazy.");
                    throw new OrderSimulationException("Nie udało się zapisać symulowanego zamówienia.", ex);
                }

                // weryfikacja - załaduj itemy i zaloguj liczbę
                await db.Entry(order).Collection(o => o.Items).LoadAsync();
                await db.Entry(order).Reference(o => o.Shipping).LoadAsync();
                logger.LogInformation("Utworzono symulowane zamówienie Id={OrderId} with {Items} items and total {Total}",
                    order.Id, order.Items.Count, order.TotalPrice);

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