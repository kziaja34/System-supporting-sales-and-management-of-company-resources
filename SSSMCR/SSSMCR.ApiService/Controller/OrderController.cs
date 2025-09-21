using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Model.Common;
using SSSMCR.ApiService.Services.Interfaces;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
}

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<PageResponse<OrderListItemDto>>> GetOrders(
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        [FromQuery] string sort = "priority,desc")
    {
        var result = await _orderService.GetPagedAsync(page, size, sort);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDetailsDto>> GetOrder(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order == null) return NotFound();

        return Ok(ToDetailsDto(order));
    }
    
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
    {
        try
        {
            var success = await _orderService.UpdateStatusAsync(id, newStatus);
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
        return new OrderDetailsDto(
            Id: order.Id,
            CustomerEmail: order.CustomerEmail,
            CustomerName: order.CustomerName,
            CreatedAt: order.CreatedAt,
            Status: order.Status.ToString(),
            Priority: order.Priority,
            Items: order.Items.Select(i => new OrderItemDto(
                ProductId: i.ProductId,
                ProductName: i.Product.Name,
                Quantity: i.Quantity,
                UnitPrice: i.UnitPrice,
                LineTotal: i.TotalPrice
            )),
            TotalPrice: order.Items.Sum(i => i.TotalPrice)
        );
    }
}