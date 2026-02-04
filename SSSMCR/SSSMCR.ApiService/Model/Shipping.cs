using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSMCR.ApiService.Model;

public class Shipping
{
    [Key]
    public int Id { get; set; }
    
    public string? TargetPoint { get; set; }
    
    public string? ExternalShipmentId { get; set; }
    
    public string? TrackingNumber { get; set; }
    
    public bool IsLabelGenerated { get; set; } = false;
    
    public DateTime? ShippedDate { get; set; }
}