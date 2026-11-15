using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockPilot.Web.Data;
using StockPilot.Web.Services;

namespace StockPilot.Web.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardApiController(AppDb db, StockService stock) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var totalUnits = await db.StockLevels.SumAsync(s => (int?)s.Quantity) ?? 0;
        var stockValue = await db.StockLevels
            .Join(db.Products, s => s.ProductId, p => p.Id, (s, p) => (decimal?)(s.Quantity * p.Price))
            .SumAsync() ?? 0m;
        var byWarehouse = await db.Warehouses.OrderBy(w => w.Name)
            .Select(w => new
            {
                name = w.Name,
                units = db.StockLevels.Where(s => s.WarehouseId == w.Id).Sum(s => (int?)s.Quantity) ?? 0
            }).ToListAsync();

        return Ok(new
        {
            productCount = await db.Products.CountAsync(),
            warehouseCount = await db.Warehouses.CountAsync(),
            totalUnits,
            stockValue,
            byWarehouse,
            lowStock = await stock.LowStockAsync(),
            recent = await stock.MovementsAsync(null, 8)
        });
    }
}