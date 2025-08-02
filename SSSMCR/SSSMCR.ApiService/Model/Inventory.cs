using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class Inventory
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Product")]
    public int? Product_id { get; set; }

    [ForeignKey("Branch")]
    public int? Branch_id { get; set; }

    public int? Quantity { get; set; }

    public int? Critical_threshold { get; set; }

    public DateTime? Created_at { get; set; }
}