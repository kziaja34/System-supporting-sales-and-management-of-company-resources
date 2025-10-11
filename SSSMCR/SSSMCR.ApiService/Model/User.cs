using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(255)]
    public required string LastName { get; set; }

    [Required]
    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    [Required]
    [MaxLength(255)]
    public required string Email { get; set; }

    [ForeignKey("Role")]
    public int RoleId { get; set; }

    [ForeignKey("Branch")]
    public int? BranchId { get; set; }
    
    
    public Role? Role { get; set; }
    public Branch? Branch { get; set; }
}