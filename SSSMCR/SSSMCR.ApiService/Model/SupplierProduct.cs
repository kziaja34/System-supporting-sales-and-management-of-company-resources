using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class SupplierProduct
{
    [Key] public int Id { get; set; }

    [ForeignKey("Supplier")] public int SupplierId { get; set; }
    [ForeignKey("Product")] public int ProductId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    public Supplier Supplier { get; set; }
    public Product Product { get; set; }
}