using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string First_name { get; set; }

    [Required]
    [MaxLength(255)]
    public string Last_name { get; set; }

    [Required]
    [MaxLength(255)]
    public string Password_hash { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; }

    [ForeignKey("Role")]
    public int? Role_id { get; set; }

    [ForeignKey("Branch")]
    public int? Branch_id { get; set; }

    public DateTime? Created_at { get; set; }
}