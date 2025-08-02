using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class Invoice
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Order")]
    public int? Order_id { get; set; }

    public DateTime? Generated_at { get; set; }

    [Required]
    [MaxLength(255)]
    public string File_path { get; set; }

    public DateTime? Created_at { get; set; }
}