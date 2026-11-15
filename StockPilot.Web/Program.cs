using Microsoft.EntityFrameworkCore;
using StockPilot.Web.Data;
using StockPilot.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<StockService>();
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Create the database and add sample data on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();

    if (!db.Products.Any())
    {
        var ahmedabad = new Warehouse { Name = "Ahmedabad" };
        var pune = new Warehouse { Name = "Pune" };
        var tee = new Product { Sku = "TEE-BLK-M", Name = "Black tee (M)", ReorderLevel = 20 };
        var mug = new Product { Sku = "MUG-WHT", Name = "White mug", ReorderLevel = 10 };
        db.AddRange(ahmedabad, pune, tee, mug);
        db.SaveChanges();

        db.StockLevels.AddRange(
            new StockLevel { ProductId = tee.Id, WarehouseId = ahmedabad.Id, Quantity = 12 },
            new StockLevel { ProductId = mug.Id, WarehouseId = ahmedabad.Id, Quantity = 40 });
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
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Products}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();