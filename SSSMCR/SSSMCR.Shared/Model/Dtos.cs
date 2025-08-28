using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SSSMCR.Shared.Model;

public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public OrderStatusDto Status { get; set; }
    public int Priority { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public decimal Total 
        => Items.Sum(i => i.Quantity * i.UnitPrice);
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    // StockAlerts pomijamy na razie
}

public sealed class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; } = false;
}

public sealed class TokenResponse
{
    public string AccessToken { get; set; } = "";
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
}

public class UserUpdateRequest
{
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    public string? Phone { get; set; }
}

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = "";
    [Required] public string NewPassword { get; set; } = "";
}

public enum OrderStatusDto
{
    Pending,
    Processing,
    Completed,
    Cancelled
}

public enum SupplyOrderStatusDto
{
    Draft,
    Ordered,
    Received,
    Cancelled
}