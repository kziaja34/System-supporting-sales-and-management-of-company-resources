using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Model.Exceptions;
using SSSMCR.ApiService.Services;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/warehouse")]
[Authorize]
public class WarehouseController(IWarehouseService svc, IReservationService reservationSvc) : ControllerBase
{
    private readonly IReservationService _reservationSvc = reservationSvc;

    [HttpPost("orders/{orderId}/reserve")]
    [Authorize(Roles = "Manager,Seller")]
    public async Task<IActionResult> Reserve(int orderId, [FromBody] int? preferredBranchId)
    {
        try
        {
            var result = await svc.ReserveForOrderAsync(orderId, preferredBranchId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unexpected error", details = ex.Message });
        }
    }

    [HttpPost("orders/{orderId}/fulfill")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Fulfill(int orderId)
    {
        try
        {
            await svc.FulfillReservationsAsync(orderId);
            return NoContent();
        }
        catch (ReservationNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unexpected error", details = ex.Message });
        }
    }

    [HttpPost("orders/{orderId}/fulfill/{branchId}")]
    [Authorize(Roles = "Manager,WarehouseWorker")]
    public async Task<IActionResult> FulfillForBranch(int orderId, int branchId)
    {
        try
        {
            await svc.FulfillReservationForBranchAsync(orderId, branchId, HttpContext.RequestAborted);
            return NoContent();
        }
        catch (ReservationNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unexpected error", details = ex.Message });
        }
    }

    [HttpPost("orders/{orderId}/release")]
    public async Task<IActionResult> Release(int orderId, [FromQuery] bool confirm = false)
    {
        try
        {
            await svc.ReleaseReservationsForOrderAsync(orderId, confirm);
            return NoContent();
        }
        catch (PartialReleaseConfirmationRequiredException ex)
        {
            return Conflict(new { message = ex.Message, requireConfirm = true });
        }
        catch (ReservationNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unexpected error", details = ex.Message });
        }
    }


    [HttpGet("reservations")]
    public async Task<IActionResult> GetReservations([FromQuery] int? branchId = null)
    {
        try
        {
            var query = await _reservationSvc.GetReservations(branchId);

            var reservations = query.Select(ToResponse);
            
            return Ok(reservations);
        }
        catch (ReservationNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unexpected error", details = ex.Message });       
        }
    }
    
    private static ReservationDto ToResponse(StockReservation r) => new()
    {
        Id = r.Id,
        OrderId = r.OrderItem.OrderId,
        ProductName = r.ProductStock.Product.Name,
        BranchId = r.ProductStock.BranchId,
        BranchName = r.ProductStock.Branch.Name,
        Quantity = r.Quantity,
        Status = r.Status.ToString(),
        CreatedAt = r.CreatedAt,
        OrderStatus = r.OrderItem.Order.Status.ToString(),
        Priority = r.OrderItem.Order.Priority.ToString(),
        CustomerName = r.OrderItem.Order.CustomerName,
        ShippingAddress = "testowy adres"
    };
}