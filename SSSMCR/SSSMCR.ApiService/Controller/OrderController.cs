using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/orders")]
[Authorize (Roles = "Administrator, Seller, Manager")]
public class OrderController(IOrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResponse<OrderListItemDto>>> GetOrders(
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        [FromQuery] string sort = "priority,desc",
        [FromQuery] string? search = null)
    {
        var result = await orderService.GetPagedAsync(page, size, sort, search);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDetailsDto>> GetOrder(int id)
    {
        try
        {
            var order = await orderService.GetByIdAsync(id);

            return Ok(ToDetailsDto(order));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
    
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
    {
        try
        {
            var success = await orderService.UpdateStatusAsync(id, newStatus);
            if (!success) return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }


    private static OrderDetailsDto ToDetailsDto(Order order)
    {
        return new OrderDetailsDto()
        {
            Id = order.Id,
            CustomerEmail = order.CustomerEmail,
            CustomerName = order.CustomerName,
            CreatedAt = order.CreatedAt,
            Status = order.Status.ToString(),
            Priority = order.Priority,
            Items = order.Items.Select(i => new OrderItemDto()
                {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.TotalPrice
                }
            ),
            TotalPrice = order.Items.Sum(i => i.TotalPrice),
            ShippingAddress = order.ShippingAddress
        };
    }
}