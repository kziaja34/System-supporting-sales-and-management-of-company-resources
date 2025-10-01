using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SSSMCR.ApiService.Model;

[Index(nameof(ProductId), nameof(BranchId), IsUnique = true)]
public class ProductStock
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Product")]
    public int ProductId { get; set; }

    [ForeignKey("Branch")]
    public int BranchId { get; set; }

    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int CriticalThreshold { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Product Product { get; set; }
    public Branch Branch { get; set; }
    
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}