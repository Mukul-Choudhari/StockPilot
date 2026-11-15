using Microsoft.AspNetCore.Mvc;

namespace StockPilot.Web.Controllers;

public class ProductsController : Controller { public IActionResult Index() => View(); }