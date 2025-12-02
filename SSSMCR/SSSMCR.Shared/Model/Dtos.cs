using System.ComponentModel.DataAnnotations;

namespace SSSMCR.Shared.Model;

// Company DTO

public class CompanyRequest
{
    [Required(ErrorMessage = "Company name is required.")]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(20)]
    public string? TaxIdentificationNumber { get; set; }

    [MaxLength(34)]
    public string? BankAccountNumber { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class CompanyResponse
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? TaxIdentificationNumber { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

// Enums

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

// Read models as record class with init; properties
public record OrderListItemDto
{
    public int Id { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Importance { get; set; } = string.Empty;
    public double ULow { get; set; }
    public double UHigh { get; set; }
    public double UMedium { get; set; }
    public int ItemsCount { get; init; }
    public decimal TotalPrice { get; init; }
}

public record OrderItemDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

public record OrderDetailsDto
{
    public int Id { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public double Priority { get; init; }
    public IEnumerable<OrderItemDto> Items { get; init; } = Array.Empty<OrderItemDto>();
    public decimal TotalPrice { get; init; }
    public string ShippingAddress { get; init; } = string.Empty;
}

public class OrderSimulationResult
{
    public int OrderId { get; set; }
    public decimal Total { get; set; }
    public int ItemsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Paged wrapper kept as class for mutable paging fields + computed properties
public class PageResponse<T>
{
    public IEnumerable<T>? Items { get; set; }
    public int Page { get; set; }
    public int TotalElements { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext => Page < TotalPages - 1;
    public bool HasPrevious => Page > 0;
}

// Responses can stay as class; keeping shape unchanged
public class ProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Requests kept as class for binding + DataAnnotations
public class ProductCreateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
}

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}

public sealed class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken => null;
    public DateTime? ExpiresAtUtc { get; set; }
}

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required] public string NewPassword { get; set; } = string.Empty;
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
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public int RoleId { get; set; }
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? RoleName { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class RoleResponse(string? name, int id)
{
    [Required]
    public int Id { get; set; } = id;

    [Required]
    public string? Name { get; set; } = name;
}

public class BranchResponse
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string? Name { get; set; }
    [Required]
    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class BranchCreateRequest
{
    [Required, MaxLength(255)]
    public string? Name { get; set; }
    [Required, MaxLength(500)]
    public string? Location { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class ReservationDto
{
    public int ReservationId { get; set; }
    public int OrderId { get; set; }
    public int BranchId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string Importance { get; init; } = string.Empty;
    
    public double ULow { get; set; }
    public double UHigh { get; set; }
    public double UMedium { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? ProductName { get; set; } = string.Empty;
    public string? BranchName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
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

// Results as record class with init; properties
public record  ReserveResult(List<ReserveLineResult> PerItemReport, bool IsPartial)
{
    
}

public record  ReserveLineResult(string ProductName, string BranchName, int ReservedQuantity, int MissingQuantity)
{
    
}

// Supply order DTOs kept with existing names to avoid breaking changes
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

public class SupplierProductResponse
{
    [Required]
    public int ProductId { get; set; }

    public decimal? Price { get; set; }
}

// Reports DTOs as record class with properties
public record SalesByBranchDto(decimal Total, string Branch);

public record SalesTrendDto(DateTime Date, decimal Total);

public class SupplierProductUpsertDto
{
    [Required]
    public int ProductId { get; set; }
    public decimal? Price { get; set; }
}

public class SupplierProductsUpdateRequest
{
    [Required]
    public List<SupplierProductUpsertDto> Items { get; set; } = new();
}

// Cache
public sealed class OrderFuzzyStats
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemsCount { get; set; }
    public decimal TotalPrice { get; set; }
}
