using Microsoft.AspNetCore.Mvc;

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
    [HttpGet("{id}")]
    public ActionResult<OrderDto> GetById(int id)
    {
        var order = new OrderDto
        {
            Id = id,
            CustomerName = "John Doe",
            OrderDate = DateTime.UtcNow,
            Total = 199.99m
        };

        return Ok(order);
    }
}