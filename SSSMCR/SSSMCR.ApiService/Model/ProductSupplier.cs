using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class ProductSupplier
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Product")]
    public int? Product_id { get; set; }

    [ForeignKey("Supplier")]
    public int? Supplier_id { get; set; }

    public DateTime? Created_at { get; set; }
}