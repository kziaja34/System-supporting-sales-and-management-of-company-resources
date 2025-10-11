using System.ComponentModel.DataAnnotations;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Model;

public class Order
{
    [Key]
    public int Id { get; set; }
    
    public int? BranchId { get; set; }

    [Required]
    [MaxLength(255)]
    public required string CustomerName { get; set; }

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public required string CustomerEmail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public OrderStatus Status { get; set; }

    public int Priority { get; set; }
    
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    
    public Branch? Branch { get; set; }
    [MaxLength(100)]
    public required string ShippingAddress { get; set; }
}