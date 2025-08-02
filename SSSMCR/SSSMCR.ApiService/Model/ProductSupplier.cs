using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class ProductSupplier
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Product")]
    public int ProductId { get; set; }

    [ForeignKey("Supplier")]
    public int SupplierId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Product Product { get; set; }
    public Supplier Supplier { get; set; }
}