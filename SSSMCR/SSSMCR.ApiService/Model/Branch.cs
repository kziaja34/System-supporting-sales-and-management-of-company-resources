using System.ComponentModel.DataAnnotations;

namespace SSSMCR.ApiService.Model;

public class Branch
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(500)]
    public required string Location { get; set; }
    
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public ICollection<ProductStock> Inventories { get; set; } = new List<ProductStock>();
    public ICollection<SupplyOrder> SupplyOrders { get; set; } = new List<SupplyOrder>();
    public ICollection<User> Users { get; set; } = new List<User>();
}