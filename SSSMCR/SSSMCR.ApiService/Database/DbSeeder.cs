using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Database;

public static class DbSeeder
{
    public static Task Seed(AppDbContext context, IServiceProvider services)
    {
        using var scope = services.CreateScope();
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
        var branch1 = new Branch { Name = "Oddział Katowice", Location = "Katowice, ul. Rozdzienskiego 1", Latitude = 50.264301, Longitude = 19.025333};
        var branch2 = new Branch { Name = "Oddział Gliwice", Location = "Gliwice, ul. Słoneczna 2", Latitude = 50.286375, Longitude = 18.616104 };
        var branch3 = new Branch { Name = "Oddział Katowice 2", Location = "Katowice, ul. Rozdzienskiego 2", Latitude = 50.264399, Longitude = 19.025336};
        context.Branches.AddRange(branch1, branch2, branch3);

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
            new ProductStock { Product = product1, Branch = branch1, Quantity = 100, ReservedQuantity = 0, CriticalThreshold = product1.BaseCriticalThreshold },
            new ProductStock { Product = product2, Branch = branch1, Quantity = 0, ReservedQuantity = 0, CriticalThreshold = product2.BaseCriticalThreshold },
            new ProductStock { Product = product3, Branch = branch1, Quantity = 0, ReservedQuantity = 0, CriticalThreshold = product3.BaseCriticalThreshold },
            new ProductStock { Product = product1, Branch = branch2, Quantity = 0, ReservedQuantity = 0, CriticalThreshold = product1.BaseCriticalThreshold },
            new ProductStock { Product = product2, Branch = branch2, Quantity = 100, ReservedQuantity = 0, CriticalThreshold = product2.BaseCriticalThreshold },
            new ProductStock { Product = product3, Branch = branch2, Quantity = 0, ReservedQuantity = 0, CriticalThreshold = product3.BaseCriticalThreshold },
            new ProductStock { Product = product1, Branch = branch3, Quantity = 0, ReservedQuantity = 0, CriticalThreshold = product1.BaseCriticalThreshold },
            new ProductStock { Product = product2, Branch = branch3, Quantity = 0, ReservedQuantity = 0, CriticalThreshold = product2.BaseCriticalThreshold },
            new ProductStock { Product = product3, Branch = branch3, Quantity = 100, ReservedQuantity = 0, CriticalThreshold = product3.BaseCriticalThreshold }
        );

        // Supplier
        var supplier1 = new Supplier
        {
            Name = "TechSupplies Ltd",
            ContactEmail = "kontakt@techsupplies.pl",
            Phone = "123-456-789",
            Address = "Warszawa, ul. Nowa 3",
            Products = new List<SupplierProduct>()
        };
        context.Suppliers.Add(supplier1);
        
        var supplierProducts = new List<SupplierProduct>
        {
            new SupplierProduct { Supplier = supplier1, Product = product1, Price = 3300m },
            new SupplierProduct { Supplier = supplier1, Product = product2, Price = 1100m }
        };
        context.SupplierProducts.AddRange(supplierProducts);
        
        // if (!context.Orders.Any())
        // {
        //     var order1 = new Order
        //     {
        //         CustomerName = "Jan Kowalski",
        //         CustomerEmail = "jan.kowalski@gmail.com",
        //         CreatedAt = DateTime.UtcNow.AddDays(-2),
        //         Status = OrderStatus.Processing,
        //         Branch = branch1,
        //         ShippingAddress = "Kraków, ul. Floriańska 10",
        //         Items = new List<OrderItem>
        //         {
        //             new OrderItem { Product = product1, UnitPrice = product1.UnitPrice, Quantity = 1 },
        //             new OrderItem { Product = product2, UnitPrice = product2.UnitPrice, Quantity = 2 }
        //         }
        //     };
        //     // Ręczne wyliczenie sumarycznych pól z klasy Order
        //     order1.ItemsCount = order1.Items.Sum(i => i.Quantity);
        //     order1.TotalPrice = order1.Items.Sum(i => i.UnitPrice * i.Quantity);
        //
        //     var order2 = new Order
        //     {
        //         CustomerName = "Marta Nowak",
        //         CustomerEmail = "m.nowak@firmowy.pl",
        //         CreatedAt = DateTime.UtcNow.AddHours(-5),
        //         Status = OrderStatus.Processing,
        //         Branch = branch2,
        //         ShippingAddress = "Gliwice, ul. Zwycięstwa 5",
        //         Items = new List<OrderItem>
        //         {
        //             new OrderItem { Product = product3, UnitPrice = product3.UnitPrice, Quantity = 10 }
        //         }
        //     };
        //     order2.ItemsCount = order2.Items.Sum(i => i.Quantity);
        //     order2.TotalPrice = order2.Items.Sum(i => i.UnitPrice * i.Quantity);
        //
        //     var order3 = new Order
        //     {
        //         CustomerName = "Piotr Zieliński",
        //         CustomerEmail = "p.zielinski@poczta.pl",
        //         CreatedAt = DateTime.UtcNow.AddDays(-7),
        //         Status = OrderStatus.Processing,
        //         Branch = branch1,
        //         ShippingAddress = "Katowice, ul. Mariacka 1",
        //         Items = new List<OrderItem>
        //         {
        //             new OrderItem { Product = product1, UnitPrice = product1.UnitPrice, Quantity = 2 },
        //             new OrderItem { Product = product3, UnitPrice = product3.UnitPrice, Quantity = 5 }
        //         }
        //     };
        //     order3.ItemsCount = order3.Items.Sum(i => i.Quantity);
        //     order3.TotalPrice = order3.Items.Sum(i => i.UnitPrice * i.Quantity);
        //
        //     context.Orders.AddRange(order1, order2, order3);
        // }
        
        context.SaveChanges();
        return Task.CompletedTask;
    }
}
