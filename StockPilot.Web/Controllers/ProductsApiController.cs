using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockPilot.Web.Data;

namespace StockPilot.Web.Controllers;

public class ProductInput
{
    [Required, StringLength(40)] public string Sku { get; set; } = "";
    [Required, StringLength(120)] public string Name { get; set; } = "";
    [StringLength(60)] public string Category { get; set; } = "General";
    [Range(0, 1_000_000)] public decimal Price { get; set; }
    [StringLength(300)] public string? ImageUrl { get; set; }
    [Range(0, 100_000)] public int ReorderLevel { get; set; }
}

public record ProductDto(int Id, string Sku, string Name, string Category, decimal Price, string? ImageUrl, int ReorderLevel, int Total);

[ApiController]
[Route("api/products")]
public class ProductsApiController(AppDb db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? q, string? category, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Name.Contains(q) || p.Sku.Contains(q));
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(p => p.Category == category);

        var total = await query.CountAsync();
        var items = await query.OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.Category, p.Price, p.ImageUrl, p.ReorderLevel,
                p.Stock.Sum(s => s.Quantity)))
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var product = await db.Products.Where(p => p.Id == id)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.Category, p.Price, p.ImageUrl, p.ReorderLevel,
                p.Stock.Sum(s => s.Quantity)))
            .FirstOrDefaultAsync();
        if (product is null) return NotFound();

        var byWarehouse = await db.StockLevels.Where(s => s.ProductId == id)
            .Join(db.Warehouses, s => s.WarehouseId, w => w.Id,
                (s, w) => new { warehouseId = w.Id, warehouse = w.Name, quantity = s.Quantity })
            .ToListAsync();

        return Ok(new { product, byWarehouse });
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(ProductInput input)
    {
        if (await db.Products.AnyAsync(p => p.Sku == input.Sku.Trim()))
            return Conflict(new { error = "SKU already exists." });

        var p = new Product
        {
            Sku = input.Sku.Trim(),
            Name = input.Name.Trim(),
            Category = input.Category.Trim(),
            Price = input.Price,
            ImageUrl = input.ImageUrl,
            ReorderLevel = input.ReorderLevel
        };
        db.Products.Add(p);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = p.Id }, new { p.Id });
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, ProductInput input)
    {
        var p = await db.Products.FindAsync(id);
        if (p is null) return NotFound();
        if (await db.Products.AnyAsync(x => x.Sku == input.Sku.Trim() && x.Id != id))
            return Conflict(new { error = "SKU already exists." });

        p.Sku = input.Sku.Trim(); p.Name = input.Name.Trim(); p.Category = input.Category.Trim();
        p.Price = input.Price; p.ImageUrl = input.ImageUrl; p.ReorderLevel = input.ReorderLevel;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.Products.FindAsync(id);
        if (p is null) return NotFound();
        if (await db.StockMovements.AnyAsync(m => m.ProductId == id))
            return BadRequest(new { error = "This product has stock history and cannot be deleted." });

        db.Products.Remove(p);
        await db.SaveChangesAsync();
        return NoContent();
    }
}