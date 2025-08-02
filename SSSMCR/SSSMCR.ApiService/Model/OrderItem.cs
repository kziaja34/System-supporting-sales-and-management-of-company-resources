using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class OrderItem
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Order")]
    public int? Order_id { get; set; }

    [ForeignKey("Product")]
    public int? Product_id { get; set; }

    public int? Quantity { get; set; }

    [Required]
    [MaxLength(255)]
    public string Unit_price { get; set; }

    public DateTime? Created_at { get; set; }
}