using System.ComponentModel.DataAnnotations;

namespace SSSMCR.ApiService.Model;

public class Supplier
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    [Required]
    [MaxLength(255)]
    public string ContactEmail { get; set; }

    [Required]
    [MaxLength(50)]
    public string Phone { get; set; }

    [Required]
    [MaxLength(500)]
    public string Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<ProductSupplier> ProductSuppliers { get; set; } = new List<ProductSupplier>();
}