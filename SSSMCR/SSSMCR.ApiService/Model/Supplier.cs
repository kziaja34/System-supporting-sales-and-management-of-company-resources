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
    public string Contact_email { get; set; }

    [Required]
    [MaxLength(255)]
    public string Phone { get; set; }

    [Required]
    [MaxLength(255)]
    public string Address { get; set; }

    public DateTime? Created_at { get; set; }
}