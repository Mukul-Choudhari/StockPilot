using Microsoft.EntityFrameworkCore;
using StockPilot.Web.Data;
using StockPilot.Web.Services;
using Xunit;

namespace StockPilot.Tests;

public class StockServiceTests
{
    // Each test gets its own fresh in-memory database
    private static (AppDb db, StockService service) Setup()
    {
        var options = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDb(options);
        db.Warehouses.AddRange(new Warehouse { Id = 1, Name = "A" }, new Warehouse { Id = 2, Name = "B" });
        db.Products.Add(new Product { Id = 1, Sku = "A", Name = "Item A", ReorderLevel = 10 });
        db.StockLevels.Add(new StockLevel { ProductId = 1, WarehouseId = 1, Quantity = 15 });
        db.SaveChanges();

        return (db, new StockService(db));
    }

    [Fact]
    public async Task Transfer_MovesQuantityBetweenWarehouses()
    {
        var (db, service) = Setup();

        await service.TransferAsync(1, 1, 2, 5);

        Assert.Equal(10, db.StockLevels.Single(s => s.WarehouseId == 1).Quantity);
        Assert.Equal(5, db.StockLevels.Single(s => s.WarehouseId == 2).Quantity);
    }

    [Fact]
    public async Task Transfer_RejectsMoreThanAvailable_AndChangesNothing()
    {
        var (db, service) = Setup();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.TransferAsync(1, 1, 2, 99));

        Assert.Equal(15, db.StockLevels.Single(s => s.WarehouseId == 1).Quantity);
    }

    [Fact]
    public async Task LowStock_ReturnsOnlyItemsBelowReorderLevel()
    {
        var (_, service) = Setup();

        Assert.Empty(await service.LowStockAsync());   // 15 is above 10

        await service.AdjustAsync(1, 1, -10);          // now 5, below 10

        Assert.Single(await service.LowStockAsync());
    }

    [Fact]
    public async Task Transfer_ToMissingWarehouse_IsRejected()
    {
        var (db, service) = Setup();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.TransferAsync(1, 1, 99, 1));

        Assert.Equal(0, db.StockMovements.Count());
    }

    [Fact]
    public async Task Transfer_RecordsTwoMovements()
    {
        var (db, service) = Setup();

        await service.TransferAsync(1, 1, 2, 5, "tester");

        var movements = db.StockMovements.OrderBy(m => m.Id).ToList();
        Assert.Equal(2, movements.Count);
        Assert.Equal(MovementType.TransferOut, movements[0].Type);
        Assert.Equal(MovementType.TransferIn, movements[1].Type);
    }
}