using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class SupplyOrder
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Supplier")]
    public int? Supplier_id { get; set; }

    [ForeignKey("Branch")]
    public int? Branch_id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Status { get; set; }

    public DateTime? Created_at { get; set; }
}