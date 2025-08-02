using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    [Required]
    [MaxLength(255)]
    public string Description { get; set; }

    [Required]
    [MaxLength(255)]
    public string Unit_price { get; set; }

    [ForeignKey("Category")]
    public int? Category_id { get; set; }

    public DateTime? Created_at { get; set; }
}