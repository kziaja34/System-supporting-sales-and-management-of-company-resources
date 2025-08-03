using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class Invoice
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Order")]
    public int OrderId { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; }
    
    public Order Order { get; set; }
}