using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/orders")]
[Authorize (Roles = "Administrator, Seller, Manager")]
public class OrderController(IOrderService orderService, IOrderSimulationService orderSimulationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResponse<OrderListItemDto>>> GetOrders(
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        [FromQuery] string sort = "id,asc",
        [FromQuery] string? search = null,
        [FromQuery] string? importance = null)
    {
        var result = await orderService.GetPagedAsync(page, size, sort, search, importance);
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
    
    [HttpPost("simulate")]
    [Authorize(Roles = "Manager,Administrator")]
    public async Task<IActionResult> Simulate()
    {
        try
        {
            var result = await orderSimulationService.SimulateOrderAsync(minProducts: 1, maxProducts: 3, minQty: 1, maxQty: 5);
            return Ok(new
            {
                orderId = result.OrderId,
                itemsCount = result.ItemsCount,
                createdAt = result.CreatedAt
            });
        }
        catch (NoProductsAvailableException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (OrderSimulationException ex)
        {
            return StatusCode(500, new { message = "Błąd podczas tworzenia symulowanego zamówienia." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Wystąpił nieoczekiwany błąd." });
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