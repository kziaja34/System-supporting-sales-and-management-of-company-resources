using System.ComponentModel.DataAnnotations;

namespace SSSMCR.ApiService.Model;

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Customer_name { get; set; }

    [Required]
    [MaxLength(255)]
    public string Customer_email { get; set; }

    public DateTime? Created_at { get; set; }

    [Required]
    [MaxLength(255)]
    public string Status { get; set; }

    public int? Priority { get; set; }
}