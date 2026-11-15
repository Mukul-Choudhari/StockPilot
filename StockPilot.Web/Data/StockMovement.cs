namespace StockPilot.Web.Data;

public enum MovementType { InitialStock, Adjustment, TransferIn, TransferOut, Receipt, Sale }

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public int Delta { get; set; }
    public MovementType Type { get; set; }
    public string? Note { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}