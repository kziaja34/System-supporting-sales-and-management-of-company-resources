using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class StockAlert
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Inventory")]
    public int InventoryId { get; set; }

    public bool Seen { get; set; } = false;

    public int? SeenBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Inventory Inventory { get; set; }
    public User SeenByUser { get; set; }
}