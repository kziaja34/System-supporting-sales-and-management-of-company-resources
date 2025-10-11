using System.ComponentModel.DataAnnotations;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Model;

public class StockMovement
{
    public int Id { get; set; }
    public int ProductStockId { get; set; }
    public int QuantityDelta { get; set; }
    public StockMovementType Type { get; set; }
    public int? OrderItemId { get; set; }
    [MaxLength(100)]
    public string? Reference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ProductStock ProductStock { get; set; } = null!;
    public OrderItem? OrderItem { get; set; }
}