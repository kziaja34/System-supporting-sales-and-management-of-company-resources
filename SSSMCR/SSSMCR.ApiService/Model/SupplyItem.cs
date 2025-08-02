using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class SupplyItem
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("SupplyOrder")]
    public int SupplyOrderId { get; set; }

    [ForeignKey("Product")]
    public int ProductId { get; set; }

    public int Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public SupplyOrder SupplyOrder { get; set; }
    public Product Product { get; set; }
}