using Microsoft.EntityFrameworkCore;
using StockPilot.Web.Data;

namespace StockPilot.Web.Services;

public record LowStockItem(string Sku, string Name, int ReorderLevel, int Total);

public class StockService(AppDb db)
{
    // Changes the quantity in memory only. Nothing is saved to the database yet.
    private async Task ApplyAsync(int productId, int warehouseId, int delta)
    {
        var stock = db.StockLevels.Local
                        .FirstOrDefault(x => x.ProductId == productId && x.WarehouseId == warehouseId)
                    ?? await db.StockLevels
                        .FirstOrDefaultAsync(x => x.ProductId == productId && x.WarehouseId == warehouseId);

        if (stock is null)
        {
            stock = new StockLevel { ProductId = productId, WarehouseId = warehouseId };
            db.StockLevels.Add(stock);
        }

        if (stock.Quantity + delta < 0)
            throw new InvalidOperationException("Insufficient stock.");

        stock.Quantity += delta;
    }

    public async Task AdjustAsync(int productId, int warehouseId, int delta)
    {
        await ApplyAsync(productId, warehouseId, delta);
        await db.SaveChangesAsync();
    }

    public async Task TransferAsync(int productId, int fromWarehouseId, int toWarehouseId, int quantity)
    {
        if (quantity <= 0 || fromWarehouseId == toWarehouseId)
            throw new ArgumentException("Invalid transfer.");

        await ApplyAsync(productId, fromWarehouseId, -quantity);
        await ApplyAsync(productId, toWarehouseId, quantity);
        await db.SaveChangesAsync(); // one save: both changes succeed, or neither does
    }

    public Task<List<LowStockItem>> LowStockAsync() =>
        db.Products
            .Select(p => new LowStockItem(p.Sku, p.Name, p.ReorderLevel, p.Stock.Sum(s => s.Quantity)))
            .Where(x => x.Total < x.ReorderLevel)
            .ToListAsync();
}