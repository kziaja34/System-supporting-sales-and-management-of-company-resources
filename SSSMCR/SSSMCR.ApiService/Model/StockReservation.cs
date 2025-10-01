using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Model;

public class StockReservation
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    public int ProductStockId { get; set; }
    public int Quantity { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReleasedAt { get; set; }
    public DateTime? FulfilledAt { get; set; }

    public OrderItem OrderItem { get; set; } = null!;
    public ProductStock ProductStock { get; set; } = null!;
}
