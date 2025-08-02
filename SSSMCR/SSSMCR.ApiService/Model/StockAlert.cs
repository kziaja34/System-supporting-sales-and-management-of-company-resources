using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class StockAlert
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Inventory")]
    public int? Inventory_id { get; set; }

    public bool? Seen { get; set; }

    public int? Seen_by { get; set; }

    public DateTime? Created_at { get; set; }
}
