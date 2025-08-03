using System.ComponentModel.DataAnnotations;

namespace SSSMCR.ApiService.Model;

public class Branch
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    [Required]
    [MaxLength(500)]
    public string Location { get; set; }
    
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<SupplyOrder> SupplyOrders { get; set; } = new List<SupplyOrder>();
    public ICollection<User> Users { get; set; } = new List<User>();
}