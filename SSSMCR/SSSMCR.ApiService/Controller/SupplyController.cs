using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/supply")]
public class SupplyController : ControllerBase
{
    private readonly ISupplyService _supplyService;

    public SupplyController(ISupplyService supplyService)
    {
        _supplyService = supplyService;
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(CancellationToken ct)
    {
        var orders = await _supplyService.GetOrdersAsync(ct);
        return Ok(orders.Select(ToResponse));
    }

    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetOrder(int orderId, CancellationToken ct)
    {
        var order = await _supplyService.GetOrderByIdAsync(orderId, ct);
        if (order == null) return NotFound();
        return Ok(ToResponse(order));
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] SupplyOrderCreateDto dto, CancellationToken ct)
    {
        try
        {
            var order = await _supplyService.CreateOrderAsync(dto.SupplierId, dto.BranchId,
                dto.Items.Select(i => (i.ProductId, i.Quantity)).ToList(), ct);

            return Ok(ToResponse(order));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpPost("orders/{orderId}/receive")]
    public async Task<IActionResult> ReceiveOrder(int orderId, CancellationToken ct)
    {
        try
        {
            await _supplyService.ReceiveOrderAsync(orderId, ct);
            var order = await _supplyService.GetOrderByIdAsync(orderId, ct);
            return Ok(ToResponse(order!));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    
    private static SupplyOrderResponseDto ToResponse(SupplyOrder order) => new()
    {
        Id = order.Id,
        SupplierName = order.Supplier?.Name ?? "Unknown",  // Zapewnienie, że nie jest null
        BranchName = order.Branch?.Name ?? "Unknown",  // Zapewnienie, że nie jest null
        OrderedAt = order.CreatedAt,
        ReceivedAt = order.ReceivedAt,
        Status = order.Status.ToString(),
        Items = order.Items.Select(i => new SupplyItemResponseDto
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? "Unknown",  // Upewnienie się, że Product nie jest null
            Quantity = i.Quantity
        }).ToList()
    };

    private static SupplyOrder ToEntity(SupplyOrderCreateDto dto)
    {
        var order = new SupplyOrder
        {
            SupplierId = dto.SupplierId,
            BranchId = dto.BranchId,
            Status = SupplyOrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in dto.Items)
        {
            order.Items.Add(new SupplyItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            });
        }

        return order;
    }

}