namespace StockPilot.Web.Data;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public int ReorderLevel { get; set; }
    public List<StockLevel> Stock { get; set; } = new();
}