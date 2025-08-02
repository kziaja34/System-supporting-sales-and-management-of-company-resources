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
    [MaxLength(1000)]
    public string Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [ForeignKey("Category")]
    public int CategoryId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Category Category { get; set; }
}