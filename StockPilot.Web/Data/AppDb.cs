using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace StockPilot.Web.Data;

public class AppDb(DbContextOptions<AppDb> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockLevel> StockLevels => Set<StockLevel>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        builder.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
        builder.Entity<StockLevel>().HasOne<Product>().WithMany(p => p.Stock).HasForeignKey(s => s.ProductId);
        builder.Entity<StockLevel>().HasOne<Warehouse>().WithMany().HasForeignKey(s => s.WarehouseId);
        builder.Entity<StockLevel>().HasIndex(s => new { s.ProductId, s.WarehouseId }).IsUnique();
        // History must never disappear: block deletes that would remove movements
        builder.Entity<StockMovement>().HasOne(m => m.Product).WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StockMovement>().HasOne(m => m.Warehouse).WithMany().HasForeignKey(m => m.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}