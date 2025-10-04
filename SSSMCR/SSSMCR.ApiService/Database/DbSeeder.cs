using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Database;

public static class DbSeeder
{
    public static Task Seed(AppDbContext context, IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        
        // Company
        if (context.Companies.Any()) return Task.CompletedTask;

        var company = new Company
        {
           CompanyName = "Company Name",
           Address = "Address",
           City = "City",
           PostalCode = "Postal Code",
           TaxIdentificationNumber = "NIP",
           BankAccountNumber = "Bank account Number",
           ContactEmail = "Contact Email",
           ContactPhone = "Contact Phone"
        };
        context.Companies.Add(company);
        
        if (context.Roles.Any()) return Task.CompletedTask;

        // Roles
        var adminRole = new Role { Name = "Administrator" };
        var managerRole = new Role { Name = "Manager" };
        var sellerRole = new Role { Name = "Seller" };
        var warehouseRole = new Role { Name = "WarehouseWorker" };
        context.Roles.AddRange(adminRole, managerRole, sellerRole, warehouseRole);

        // Branches
        var branch1 = new Branch { Name = "Oddział Katowice", Location = "Katowice, ul. Główna 1" };
        var branch2 = new Branch { Name = "Oddział Gliwice", Location = "Gliwice, ul. Słoneczna 2" };
        context.Branches.AddRange(branch1, branch2);

        // Users
        var admin = new User
        {
            FirstName = "Admin",
            LastName = "System",
            Email = "admin@example.com",
            PasswordHash = hasher.Hash("hashed123"),
            Role = adminRole,
            Branch = branch1
        };
        var seller = new User
        {
            FirstName = "Anna",
            LastName = "Sprzedawczyni",
            Email = "anna@example.com",
            PasswordHash = hasher.Hash("hashed123"),
            Role = sellerRole,
            Branch = branch1
        };
        var warehouseman = new User
        {
            FirstName = "Piotr",
            LastName = "Magazynier",
            Email = "piotr@example.com",
            PasswordHash = hasher.Hash("hashed123"),
            Role = warehouseRole,
            Branch = branch2
        };
        var manager = new User()
        {
            FirstName = "Manager",
            LastName = "Super",
            Email = "manager@example.com",
            PasswordHash = hasher.Hash("hashed123"),
            Role = managerRole,
            Branch = branch1
        };
        var manager2 = new User()
        {
            FirstName = "Manager",
            LastName = "Super",
            Email = "manager2@example.com",
            PasswordHash = hasher.Hash("hashed123"),
            Role = managerRole,
            Branch = branch2
        };
        context.Users.AddRange(admin, seller, warehouseman, manager, manager2);

        // Products
        var product1 = new Product { Name = "Laptop X", Description = "Laptop 15 cali", UnitPrice = 2500m, BaseCriticalThreshold = 3 };
        var product2 = new Product { Name = "Monitor Y", Description = "Monitor 24 cale", UnitPrice = 800m, BaseCriticalThreshold = 5 };
        var product3 = new Product { Name = "Mysz Z", Description = "Mysz bezprzewodowa", UnitPrice = 120m, BaseCriticalThreshold = 10 };
        context.Products.AddRange(product1, product2, product3);

        // ProductStock
        context.ProductStock.AddRange(
            new ProductStock { Product = product1, Branch = branch1, Quantity = 10, ReservedQuantity = 0, CriticalThreshold = 3 },
            new ProductStock { Product = product2, Branch = branch1, Quantity = 5, ReservedQuantity = 0, CriticalThreshold = 2 },
            new ProductStock { Product = product3, Branch = branch2, Quantity = 20, ReservedQuantity = 0, CriticalThreshold = 5 }
        );

        // Supplier
        var supplier1 = new Supplier
        {
            Name = "TechSupplies Ltd",
            ContactEmail = "kontakt@techsupplies.pl",
            Phone = "123-456-789",
            Address = "Warszawa, ul. Nowa 3"
        };
        context.Suppliers.Add(supplier1);

        // SupplyOrder + SupplyItem
        var supplyOrder1 = new SupplyOrder
        {
            Branch = branch1,
            Supplier = supplier1,
            Status = Model.Common.SupplyOrderStatus.Ordered
        };
        context.SupplyOrders.Add(supplyOrder1);

        context.SupplyItems.AddRange(
            new SupplyItem { SupplyOrder = supplyOrder1, Product = product1, Quantity = 5 },
            new SupplyItem { SupplyOrder = supplyOrder1, Product = product2, Quantity = 3 }
        );

        // Order + OrderItem
        var order1 = new Order
        {
            CustomerName = "Jan Kowalski",
            CustomerEmail = "jan.kowalski@example.com",
            Status = OrderStatus.Pending,
            Priority = 1,
            ShippingAddress = "Warszawa, ul. Nowa 3",
        };
        context.Orders.Add(order1);

        context.OrderItems.AddRange(
            new OrderItem { Order = order1, Product = product1, Quantity = 1, UnitPrice = product1.UnitPrice }
        );
        
        var order2 = new Order
        {
            CustomerName = "Anna Nowak",
            CustomerEmail = "anna.nowak@example.com",
            Status = OrderStatus.Pending,
            Priority = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ShippingAddress = "Będzin, ul. Norwida 17"
        };
        context.Orders.Add(order2);

        context.OrderItems.AddRange(
            new OrderItem { Order = order2, Product = product2, Quantity = 1, UnitPrice = product2.UnitPrice },
            new OrderItem { Order = order2, Product = product3, Quantity = 2, UnitPrice = product3.UnitPrice }
        );

        context.SaveChanges();
        return Task.CompletedTask;
    }
}
