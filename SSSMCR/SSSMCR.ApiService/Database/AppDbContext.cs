using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductStock> ProductStock => Set<ProductStock>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierProduct> SupplierProducts => Set<SupplierProduct>();
    public DbSet<SupplyOrder> SupplyOrders => Set<SupplyOrder>();
    public DbSet<SupplyItem> SupplyItems => Set<SupplyItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Branch -> Users
        modelBuilder.Entity<User>()
            .HasOne(u => u.Branch)
            .WithMany(b => b.Users)
            .HasForeignKey(u => u.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        // Branch -> ProductStock
        modelBuilder.Entity<ProductStock>()
            .HasOne(i => i.Branch)
            .WithMany(b => b.Inventories)
            .HasForeignKey(i => i.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        // Branch -> SupplyOrder
        modelBuilder.Entity<SupplyOrder>()
            .HasOne(so => so.Branch)
            .WithMany(b => b.SupplyOrders)
            .HasForeignKey(so => so.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // OrderItem -> Product
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<ProductStock>(eb =>
        {
            eb.HasIndex(x => new { x.ProductId, x.BranchId }).IsUnique();
            eb.ToTable(t => t.HasCheckConstraint(
                "CK_ProductStock_ReservedRange",
                "[Quantity] >= 0 AND [ReservedQuantity] >= 0 AND [ReservedQuantity] <= [Quantity]"));

            eb.Property(x => x.RowVersion).IsRowVersion();
        });
    }

}