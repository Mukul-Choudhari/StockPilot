using Microsoft.AspNetCore.Mvc;

namespace StockPilot.Web.Controllers;

public class DashboardController : Controller { public IActionResult Index() => View(); }
public class WarehousesController : Controller { public IActionResult Index() => View(); }
public class MovementsController : Controller { public IActionResult Index() => View(); }