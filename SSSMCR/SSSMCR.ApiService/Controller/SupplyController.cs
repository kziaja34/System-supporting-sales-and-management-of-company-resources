using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/supply")]
[Authorize (Roles = "Administrator, WarehouseWorker, Manager")]
public class SupplyController(ISupplyService supplyService) : ControllerBase
{
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(CancellationToken ct)
    {
        var orders = await supplyService.GetOrdersAsync(ct);
        return Ok(orders.Select(ToResponse));
    }

    [HttpGet("orders/{orderId}")]
    public async Task<IActionResult> GetOrder(int orderId, CancellationToken ct)
    {
        try
        {
            var order = await supplyService.GetOrderByIdAsync(orderId, ct);
            return Ok(ToResponse(order));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] SupplyOrderCreateDto dto, CancellationToken ct)
    {
        try
        {
            var order = await supplyService.CreateOrderAsync(dto.SupplierId, dto.BranchId,
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
            await supplyService.ReceiveOrderAsync(orderId, ct);
            var order = await supplyService.GetOrderByIdAsync(orderId, ct);
            return Ok(ToResponse(order));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    
    private static SupplyOrderResponseDto ToResponse(SupplyOrder order) => new()
    {
        Id = order.Id,
        SupplierName = order.Supplier?.Name ?? "Unknown",
        BranchName = order.Branch?.Name ?? "Unknown",
        OrderedAt = order.CreatedAt,
        ReceivedAt = order.ReceivedAt,
        Status = order.Status.ToString(),
        Items = order.Items.Select(i => new SupplyItemResponseDto
        {
            ProductName = i.Product?.Name ?? "Unknown",
            Quantity = i.Quantity
        }).ToList()
    };

    // private static SupplyOrder ToEntity(SupplyOrderCreateDto dto)
    // {
    //     var order = new SupplyOrder
    //     {
    //         SupplierId = dto.SupplierId,
    //         BranchId = dto.BranchId,
    //         Status = SupplyOrderStatus.Pending,
    //         CreatedAt = DateTime.UtcNow
    //     };
    //
    //     foreach (var item in dto.Items)
    //     {
    //         order.Items.Add(new SupplyItem
    //         {
    //             ProductId = item.ProductId,
    //             Quantity = item.Quantity
    //         });
    //     }
    //
    //     return order;
    // }

}