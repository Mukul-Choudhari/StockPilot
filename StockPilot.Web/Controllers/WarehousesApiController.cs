using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockPilot.Web.Data;

namespace StockPilot.Web.Controllers;

public class WarehouseInput { [Required, StringLength(80)] public string Name { get; set; } = ""; }
public record WarehouseDto(int Id, string Name, int Units, int Products);

[ApiController]
[Route("api/warehouses")]
public class WarehousesApiController(AppDb db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.Warehouses.OrderBy(w => w.Name)
        .Select(w => new WarehouseDto(w.Id, w.Name,
            db.StockLevels.Where(s => s.WarehouseId == w.Id).Sum(s => s.Quantity),
            db.StockLevels.Count(s => s.WarehouseId == w.Id && s.Quantity > 0)))
        .ToListAsync());

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(WarehouseInput input)
    {
        if (await db.Warehouses.AnyAsync(w => w.Name == input.Name.Trim()))
            return Conflict(new { error = "A warehouse with this name already exists." });
        var w = new Warehouse { Name = input.Name.Trim() };
        db.Warehouses.Add(w);
        await db.SaveChangesAsync();
        return Created($"api/warehouses/{w.Id}", new { w.Id });
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, WarehouseInput input)
    {
        var w = await db.Warehouses.FindAsync(id);
        if (w is null) return NotFound();
        w.Name = input.Name.Trim();
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var w = await db.Warehouses.FindAsync(id);
        if (w is null) return NotFound();
        if (await db.StockMovements.AnyAsync(m => m.WarehouseId == id) ||
            await db.StockLevels.AnyAsync(s => s.WarehouseId == id && s.Quantity > 0))
            return BadRequest(new { error = "This warehouse has stock or history and cannot be deleted." });

        db.Warehouses.Remove(w);
        await db.SaveChangesAsync();
        return NoContent();
    }
}