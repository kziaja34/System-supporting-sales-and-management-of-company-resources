using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(255)]
    public string LastName { get; set; }

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; }

    [ForeignKey("Role")]
    public int RoleId { get; set; }

    [ForeignKey("Branch")]
    public int BranchId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Role Role { get; set; }
    public Branch Branch { get; set; }
}