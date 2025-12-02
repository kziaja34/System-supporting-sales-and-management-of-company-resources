using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Authorize (Roles = "Administrator")]
[Route("api/dev")]
public class DevSeedController(AppDbContext db) : ControllerBase
{
    [HttpPost("seed-orders")]
    public async Task<IActionResult> SeedOrders(CancellationToken ct)
    {
        const int OrdersCount = 10_000;
        const int ItemsPerOrder = 10;
        
        var products = await db.Products.ToListAsync(ct);
        var branches = await db.Branches.ToListAsync(ct);

        if (!products.Any() || !branches.Any())
            return BadRequest("Products or branches missing in DB.");

        var rnd = new Random();

        for (int i = 0; i < OrdersCount; i++)
        {
            var branch = branches[rnd.Next(branches.Count)];

            var order = new Order
            {
                CustomerName = $"Klient {i}",
                CustomerEmail = $"klient{i}@example.com",
                Status = OrderStatus.Pending,
                Priority = 0,
                ShippingAddress = "Adres testowy",
                CreatedAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 30)),
                Branch = branch
            };
            db.Orders.Add(order);

            var items = new List<OrderItem>();

            for (int j = 0; j < ItemsPerOrder; j++)
            {
                var product = products[rnd.Next(products.Count)];
                var qty = rnd.Next(1, 5);

                var item = new OrderItem
                {
                    Order = order,
                    Product = product,
                    Quantity = qty,
                    UnitPrice = product.UnitPrice
                };

                items.Add(item);
                db.OrderItems.Add(item);
            }
            
            order.ItemsCount = items.Count;
            order.TotalPrice = items.Sum(i => i.UnitPrice * i.Quantity);

        }

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            message = $"Seeded {OrdersCount} orders and {OrdersCount * ItemsPerOrder} items."
        });
    }
}
