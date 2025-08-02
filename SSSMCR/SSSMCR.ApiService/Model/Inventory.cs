using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class Inventory
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Product")]
    public int ProductId { get; set; }

    [ForeignKey("Branch")]
    public int BranchId { get; set; }

    public int Quantity { get; set; }

    public int CriticalThreshold { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Product Product { get; set; }
    public Branch Branch { get; set; }
    
    public ICollection<StockAlert> StockAlerts { get; set; } = new List<StockAlert>();
}