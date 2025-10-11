using System.ComponentModel.DataAnnotations;

namespace SSSMCR.ApiService.Model;

public class Supplier
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(255)]
    public required string ContactEmail { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Phone { get; set; }

    [Required]
    [MaxLength(500)]
    public required string Address { get; set; }
    
    public ICollection<SupplierProduct> Products { get; set; } = new List<SupplierProduct>();
}