using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class SupplyItem
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("SupplyOrder")]
    public int? Supply_order_id { get; set; }

    [ForeignKey("Product")]
    public int? Product_id { get; set; }

    public int? Quantity { get; set; }

    [Required]
    [MaxLength(255)]
    public string Unit_price { get; set; }

    public DateTime? Created_at { get; set; }
}