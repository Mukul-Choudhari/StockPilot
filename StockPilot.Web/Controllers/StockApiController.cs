using Microsoft.AspNetCore.Mvc;
using StockPilot.Web.Services;

namespace StockPilot.Web.Controllers;

public record TransferRequest(int ProductId, int FromWarehouseId, int ToWarehouseId, int Quantity);

[ApiController]
[Route("api/stock")]
public class StockApiController(StockService stock) : ControllerBase
{
    [HttpGet("low")]
    public async Task<IActionResult> Low() => Ok(await stock.LowStockAsync());

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(TransferRequest request)
    {
        try
        {
            await stock.TransferAsync(request.ProductId, request.FromWarehouseId,
                                      request.ToWarehouseId, request.Quantity);
            return NoContent();
        }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException)
        {
            return BadRequest(new { error = e.Message });
        }
    }
}