using System.ComponentModel.DataAnnotations;

namespace SSSMCR.ApiService.Model;

public class Role
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; }
    
    public ICollection<User> Users { get; set; } = new List<User>();
}