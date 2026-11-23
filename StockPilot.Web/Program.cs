using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using StockPilot.Web.Data;
using StockPilot.Web.Services;
using Google.GenAI;

var builder = WebApplication.CreateBuilder(args);

// Bind to Railway's dynamic PORT when running on Railway (keeps launchSettings/local ports when PORT is unset)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Database configuration
// Supports ConnectionStrings__Default (env var) or ConnectionStrings:Default (appsettings)
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default");

var isLocalDb = string.IsNullOrWhiteSpace(connectionString)
                || connectionString.Contains("localdb", StringComparison.OrdinalIgnoreCase)
                || connectionString.Contains("attachdbfilename", StringComparison.OrdinalIgnoreCase);

var hasRemoteSql = !isLocalDb;

if (hasRemoteSql)
{
    builder.Services.AddDbContext<AppDb>(o => o.UseSqlServer(connectionString));
}
else
{
    // Safe non-LocalDB placeholder string so AddDbContext doesn't fail on Linux during UI/smoke checks
    builder.Services.AddDbContext<AppDb>(o =>
        o.UseSqlServer("Server=127.0.0.1,1433;Database=StockPilotDemo;User Id=sa;Password=Placeholder_Password_123!;TrustServerCertificate=True;"));
}

// Identity configuration
builder.Services.AddIdentity<IdentityUser, IdentityRole>(o => o.Password.RequiredLength = 8)
    .AddEntityFrameworkStores<AppDb>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/Login";
    // API calls get a 401 instead of a redirect to the login page
    o.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else
            ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});

// Application Services
builder.Services.AddScoped<StockService>();

// --- REGISTER GOOGLE GEMINI CLIENT ---
builder.Services.AddSingleton<Client>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    string apiKey = configuration["Gemini:ApiKey"]
                    ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                    ?? string.Empty;

    // Only throw if someone actually calls an endpoint/service that requires the Gemini Client
    if (string.IsNullOrWhiteSpace(apiKey))
        throw new InvalidOperationException("Gemini API key is not configured. Set GEMINI_API_KEY in environment variables.");

    return new Client(apiKey: apiKey);
});
// -------------------------------------

// Every controller requires login unless marked [AllowAnonymous]
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AuthorizeFilter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Run migrations and seeding only when a real remote SQL connection string is configured
if (hasRemoteSql)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        await db.Database.MigrateAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var role in new[] { "Admin", "Warehouse" })
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));

        if (await users.FindByEmailAsync("admin@stockpilot.demo") is null)
        {
            var admin = new IdentityUser
            {
                UserName = "admin@stockpilot.demo",
                Email = "admin@stockpilot.demo",
                EmailConfirmed = true
            };
            await users.CreateAsync(admin, "Demo@12345");
            await users.AddToRoleAsync(admin, "Admin");
        }

        if (!await db.Products.AnyAsync())
        {
            var warehouses = new[]
            {
                new Warehouse { Name = "Ahmedabad" },
                new Warehouse { Name = "Pune" },
                new Warehouse { Name = "Mumbai" }
            };
            db.Warehouses.AddRange(warehouses);

            var products = new[]
            {
                new Product { Sku = "TEE-BLK-M", Name = "Black tee (M)",        Category = "Apparel",     Price = 499,  ReorderLevel = 45 },
                new Product { Sku = "HDY-GRY-L", Name = "Grey hoodie (L)",      Category = "Apparel",     Price = 1499, ReorderLevel = 45 },
                new Product { Sku = "MUG-WHT",   Name = "White ceramic mug",    Category = "Home",        Price = 299,  ReorderLevel = 45 },
                new Product { Sku = "BTL-STL",   Name = "Steel water bottle",   Category = "Home",        Price = 699,  ReorderLevel = 45 },
                new Product { Sku = "BAG-CVS",   Name = "Canvas tote bag",      Category = "Accessories", Price = 399,  ReorderLevel = 45 },
                new Product { Sku = "CAP-NVY",   Name = "Navy baseball cap",    Category = "Accessories", Price = 349,  ReorderLevel = 45 },
                new Product { Sku = "NTB-A5",    Name = "A5 notebook",          Category = "Stationery",  Price = 199,  ReorderLevel = 45 },
                new Product { Sku = "PEN-GEL",   Name = "Gel pens (pack of 10)", Category = "Stationery", Price = 149,  ReorderLevel = 45 }
            };
            db.Products.AddRange(products);
            await db.SaveChangesAsync();

            var rnd = new Random(7);
            foreach (var p in products)
                foreach (var w in warehouses)
                {
                    var qty = rnd.Next(0, 40);
                    if (qty == 0) continue;
                    db.StockLevels.Add(new StockLevel { ProductId = p.Id, WarehouseId = w.Id, Quantity = qty });
                    db.StockMovements.Add(new StockMovement
                    {
                        ProductId = p.Id,
                        WarehouseId = w.Id,
                        Delta = qty,
                        Type = MovementType.InitialStock,
                        Note = "Opening stock",
                        CreatedBy = "system"
                    });
                }
            await db.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration or seeding failed during startup.");
        throw;
    }
}
else
{
    app.Logger.LogWarning("Skipping startup DB migration/seeding because no non-LocalDB connection string is configured.");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI();

// Only run UseHttpsRedirection locally when not behind Railway's proxy
if (string.IsNullOrWhiteSpace(port))
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllers();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();