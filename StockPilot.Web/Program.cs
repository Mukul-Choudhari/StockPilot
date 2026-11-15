using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using StockPilot.Web.Data;
using StockPilot.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

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

builder.Services.AddScoped<StockService>();
// Every controller requires login unless marked [AllowAnonymous]
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AuthorizeFilter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.Migrate();

    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    foreach (var role in new[] { "Admin", "Warehouse" })
        if (!await roles.RoleExistsAsync(role))
            await roles.CreateAsync(new IdentityRole(role));

    if (await users.FindByEmailAsync("admin@stockpilot.demo") is null)
    {
        var admin = new IdentityUser { UserName = "admin@stockpilot.demo", Email = "admin@stockpilot.demo", EmailConfirmed = true };
        await users.CreateAsync(admin, "Demo@12345");
        await users.AddToRoleAsync(admin, "Admin");
    }

    if (!db.Products.Any())
    {
        var warehouses = new[] { new Warehouse { Name = "Ahmedabad" }, new Warehouse { Name = "Pune" }, new Warehouse { Name = "Mumbai" } };
        db.Warehouses.AddRange(warehouses);

        var products = new[]
        {
            new Product { Sku = "TEE-BLK-M", Name = "Black tee (M)",       Category = "Apparel",     Price = 499,  ReorderLevel = 45 },
            new Product { Sku = "HDY-GRY-L", Name = "Grey hoodie (L)",     Category = "Apparel",     Price = 1499, ReorderLevel = 45 },
            new Product { Sku = "MUG-WHT",   Name = "White ceramic mug",    Category = "Home",        Price = 299,  ReorderLevel = 45 },
            new Product { Sku = "BTL-STL",   Name = "Steel water bottle",   Category = "Home",        Price = 699,  ReorderLevel = 45 },
            new Product { Sku = "BAG-CVS",   Name = "Canvas tote bag",      Category = "Accessories", Price = 399,  ReorderLevel = 45 },
            new Product { Sku = "CAP-NVY",   Name = "Navy baseball cap",    Category = "Accessories", Price = 349,  ReorderLevel = 45 },
            new Product { Sku = "NTB-A5",    Name = "A5 notebook",          Category = "Stationery",  Price = 199,  ReorderLevel = 45 },
            new Product { Sku = "PEN-GEL",   Name = "Gel pens (pack of 10)", Category = "Stationery", Price = 149,  ReorderLevel = 45 }
        };
        db.Products.AddRange(products);
        db.SaveChanges();

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
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();