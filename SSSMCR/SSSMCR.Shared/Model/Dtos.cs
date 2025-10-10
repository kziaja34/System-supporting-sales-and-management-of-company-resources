using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SSSMCR.Shared.Model;


public enum OrderStatus
{
    Pending,
    Processing,
    PartiallyFulfilled,
    Completed,
    Cancelled
}
public enum SupplyOrderStatus
{
    Pending,
    Received,
    Cancelled
}

public enum ReservationStatus { Active, Released, Fulfilled }
public enum StockMovementType { Inbound, Outbound, Adjustment }

public record OrderListItemDto(
    int Id,
    string CustomerEmail,
    string CustomerName,
    DateTime CreatedAt,
    string Status,
    int Priority,
    int ItemsCount,
    decimal TotalPrice
);

public record OrderItemDto(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);

public record OrderDetailsDto(
    int Id,
    string CustomerEmail,
    string CustomerName,
    DateTime CreatedAt,
    string Status,
    int Priority,
    IEnumerable<OrderItemDto> Items,
    decimal TotalPrice,
    string ShippingAddress
);

public class PageResponse<T>
{
    public IEnumerable<T> Items { get; set; }
    public int Page { get; set; }
    public int Size { get; set; }
    public int TotalElements { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext => Page < TotalPages - 1;
    public bool HasPrevious => Page > 0;
}

public class ProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductCreateRequest
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal UnitPrice { get; set; }
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

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = "";
    [Required] public string NewPassword { get; set; } = "";
}

public sealed class UpdateMeRequest
{
    [Required, MaxLength(255)]
    public string FirstName { get; set; } = default!;
    [Required, MaxLength(255)]
    public string LastName { get; set; } = default!;
}

public sealed class UserCreateRequest
{
    public string FirstName { get; set; } = default!;
    [Required, MaxLength(255)]
    public string LastName { get; set; } = default!;
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = default!;
    [Required, MinLength(6), MaxLength(255)]
    public string Password { get; set; } = default!;
    [Required]
    public int RoleId { get; set; }
    [Required]
    public int BranchId { get; set; }
}

public sealed class UserUpdateRequest
{
    [Required, MaxLength(255)]
    public string FirstName { get; set; } = default!;
    [Required, MaxLength(255)]
    public string LastName { get; set; } = default!;
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = default!;
    [Required]
    public int RoleId { get; set; }
    [Required]
    public int BranchId { get; set; }
        
    [MinLength(6), MaxLength(255)]
    public string? NewPassword { get; set; }
}

public sealed class UserResponse
{
    public int Id { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string FirstName { get; set; } = default!;
    public string LastName  { get; set; } = default!;
    public string Email     { get; set; } = default!;
    public int RoleId       { get; set; }
    public int BranchId     { get; set; }
    public string BranchName { get; set; } = default!;
    public string RoleName   { get; set; } = default!;
}

public class RoleResponse
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; }
}

public class BranchResponse
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public string Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class BranchCreateRequest
{
    [Required, MaxLength(255)]
    public string Name { get; set; } = default!;
    [Required, MaxLength(500)]
    public string Location { get; set; } = default!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class ReservationDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int BranchId { get; set; }
    public string OrderStatus { get; set; } = "";
    public string Priority { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string ShippingAddress { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string BranchName { get; set; } = "";
    public int Quantity { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ProductStockDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int Available => Quantity - ReservedQuantity;
    public int CriticalThreshold { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}

public record ReserveResult(IReadOnlyList<ReserveLineResult> Lines, bool IsPartial);

public record ReserveLineResult(
    int OrderItemId,
    string ProductName,
    string BranchName,
    int ReservedQuantity,
    int MissingQuantity
);

public class SupplyOrderCreateDto
{
    public int SupplierId { get; set; }
    public int BranchId { get; set; }
    public List<SupplyItemCreateDto> Items { get; set; } = new();
}

public class SupplyItemCreateDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class SupplyOrderResponseDto
{
    public int Id { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;

    public DateTime OrderedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string Status { get; set; } = string.Empty;

    public List<SupplyItemResponseDto> Items { get; set; } = new();
}

public class SupplyItemResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class SupplierResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class SupplierCreateRequest
{
    [Required, MaxLength(255)]
    public string Name { get; set; } = default!;

    [Required, MaxLength(255), EmailAddress]
    public string ContactEmail { get; set; } = default!;

    [Required, MaxLength(50)]
    public string Phone { get; set; } = default!;

    [Required, MaxLength(500)]
    public string Address { get; set; } = default!;
}

public class SupplierUpdateRequest
{
    [Required, MaxLength(255)]
    public string Name { get; set; } = default!;

    [Required, MaxLength(255), EmailAddress]
    public string ContactEmail { get; set; } = default!;

    [Required, MaxLength(50)]
    public string Phone { get; set; } = default!;

    [Required, MaxLength(500)]
    public string Address { get; set; } = default!;
}