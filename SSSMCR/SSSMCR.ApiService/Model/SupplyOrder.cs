using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Model;

public class SupplyOrder
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Supplier")]
    public int SupplierId { get; set; }

    [ForeignKey("Branch")]
    public int BranchId { get; set; }

    [Required]
    public SupplyOrderStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ReceivedAt { get; set; }
    
    public Supplier Supplier { get; set; }
    public Branch Branch { get; set; }
    public ICollection<SupplyItem> Items { get; set; } = new List<SupplyItem>();
}