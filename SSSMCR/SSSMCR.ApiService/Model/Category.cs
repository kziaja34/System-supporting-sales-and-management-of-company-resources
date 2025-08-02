using System.ComponentModel.DataAnnotations;

namespace SSSMCR.ApiService.Model;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    public DateTime? Created_at { get; set; }
}