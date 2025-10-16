namespace SSSMCR.ApiService.Model;

public class Invoice
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
