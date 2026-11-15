using Microsoft.EntityFrameworkCore;
using StockPilot.Web.Data;

namespace StockPilot.Web.Services;

public record LowStockItem(string Sku, string Name, int ReorderLevel, int Total);
public record MovementDto(int Id, string Sku, string Product, string Warehouse, int Delta, string Type, string? Note, string? User, DateTime At);

public class StockService(AppDb db)
{
    private async Task EnsureExistsAsync(int productId, int warehouseId)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId))
            throw new KeyNotFoundException("Product not found.");
        if (!await db.Warehouses.AnyAsync(w => w.Id == warehouseId))
            throw new KeyNotFoundException("Warehouse not found.");
    }

    // Changes stock in memory and records a movement. Nothing is saved yet.
    private async Task ApplyAsync(int productId, int warehouseId, int delta, MovementType type, string? note, string? user)
    {
        var stock = db.StockLevels.Local.FirstOrDefault(x => x.ProductId == productId && x.WarehouseId == warehouseId)
                    ?? await db.StockLevels.FirstOrDefaultAsync(x => x.ProductId == productId && x.WarehouseId == warehouseId);

        if (stock is null)
        {
            stock = new StockLevel { ProductId = productId, WarehouseId = warehouseId };
            db.StockLevels.Add(stock);
        }

        if (stock.Quantity + delta < 0) throw new InvalidOperationException("Insufficient stock.");
        stock.Quantity += delta;

        db.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Delta = delta,
            Type = type,
            Note = note,
            CreatedBy = user
        });
    }

    public async Task AdjustAsync(int productId, int warehouseId, int delta, string? note = null, string? user = null)
    {
        if (delta == 0) throw new ArgumentException("Delta cannot be zero.");
        await EnsureExistsAsync(productId, warehouseId);
        await ApplyAsync(productId, warehouseId, delta, MovementType.Adjustment, note, user);
        await db.SaveChangesAsync();
    }

    public async Task TransferAsync(int productId, int fromWarehouseId, int toWarehouseId, int quantity, string? user = null)
    {
        if (quantity <= 0 || fromWarehouseId == toWarehouseId) throw new ArgumentException("Invalid transfer.");
        await EnsureExistsAsync(productId, fromWarehouseId);
        await EnsureExistsAsync(productId, toWarehouseId);

        await ApplyAsync(productId, fromWarehouseId, -quantity, MovementType.TransferOut, $"To warehouse {toWarehouseId}", user);
        await ApplyAsync(productId, toWarehouseId, quantity, MovementType.TransferIn, $"From warehouse {fromWarehouseId}", user);
        await db.SaveChangesAsync(); // one save: both stock changes and both movements succeed, or none do
    }

    public Task<List<LowStockItem>> LowStockAsync() =>
        db.Products
            .Where(p => p.Stock.Sum(s => s.Quantity) < p.ReorderLevel)
            .Select(p => new LowStockItem(p.Sku, p.Name, p.ReorderLevel, p.Stock.Sum(s => s.Quantity)))
            .ToListAsync();

    public async Task<List<MovementDto>> MovementsAsync(int? productId, int take = 50)
    {
        var rows = await db.StockMovements
            .Include(m => m.Product).Include(m => m.Warehouse)
            .Where(m => productId == null || m.ProductId == productId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync();

        return rows.Select(m => new MovementDto(m.Id, m.Product!.Sku, m.Product.Name, m.Warehouse!.Name,
            m.Delta, m.Type.ToString(), m.Note, m.CreatedBy, m.CreatedAt)).ToList();
    }
}