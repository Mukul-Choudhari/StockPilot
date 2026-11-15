namespace StockPilot.Web.Data;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "General";
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int ReorderLevel { get; set; }
    public List<StockLevel> Stock { get; set; } = new();
}