using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockPilot.Web.Data;

namespace StockPilot.Web.Controllers;

public class ProductsController(AppDb db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.Products.Include(p => p.Stock).ToListAsync());
}