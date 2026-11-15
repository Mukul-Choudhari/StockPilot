using Microsoft.AspNetCore.Mvc;
using StockPilot.Web.Services;

namespace StockPilot.Web.Controllers;

public record TransferRequest(int ProductId, int FromWarehouseId, int ToWarehouseId, int Quantity);
public record AdjustRequest(int ProductId, int WarehouseId, int Delta, string? Note);

[ApiController]
[Route("api/stock")]
public class StockApiController(StockService stock) : ControllerBase
{
    [HttpGet("low")]
    public async Task<IActionResult> Low() => Ok(await stock.LowStockAsync());

    [HttpGet("movements")]
    public async Task<IActionResult> Movements(int? productId, int take = 50) =>
        Ok(await stock.MovementsAsync(productId, take));

    [HttpPost("transfer")]
    public Task<IActionResult> Transfer(TransferRequest r) =>
        Run(() => stock.TransferAsync(r.ProductId, r.FromWarehouseId, r.ToWarehouseId, r.Quantity, User.Identity?.Name));

    [HttpPost("adjust")]
    public Task<IActionResult> Adjust(AdjustRequest r) =>
        Run(() => stock.AdjustAsync(r.ProductId, r.WarehouseId, r.Delta, r.Note, User.Identity?.Name));

    private async Task<IActionResult> Run(Func<Task> action)
    {
        try { await action(); return NoContent(); }
        catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException)
        { return BadRequest(new { error = e.Message }); }
    }
}