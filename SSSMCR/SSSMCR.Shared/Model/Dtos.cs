using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SSSMCR.Shared.Model;


public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
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
    decimal TotalPrice
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
}

public class BranchCreateRequest
{
    [Required, MaxLength(255)]
    public string Name { get; set; } = default!;
    [Required, MaxLength(500)]
    public string Location { get; set; } = default!;
}

public record ReserveResult(IReadOnlyList<ReserveLineResult> Lines, bool IsPartial);
public record ReserveLineResult(int OrderItemId, int ReservedQuantity, int MissingQuantity)
{
    public static ReserveLineResult Done(int orderItemId, int reserved, int missing) =>
        new(orderItemId, reserved, missing);
}