using System.Security.Claims;
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
public class WarehouseController(IWarehouseService svc, IReservationService reservationSvc, IOrderService orderSvc)
    : ControllerBase
{
    private readonly IReservationService _reservationSvc = reservationSvc;
    private readonly IOrderService _orderSvc = orderSvc;
    
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("orders/{orderId}/reserve")]
    [Authorize(Roles = "Manager,Seller, Administrator")]
    public async Task<IActionResult> Reserve(int orderId, [FromBody] int? preferredBranchId)
    {
        try
        {
            var result = await svc.ReserveForOrderAsync(orderId, CurrentUserId, preferredBranchId);
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

    [HttpPost("orders/{orderId}/fulfill/reservations/{reservationId}")]
    [Authorize(Roles = "Manager,WarehouseWorker, Administrator")]
    public async Task<IActionResult> FulfillReservation(int orderId, int reservationId)
    {
        try
        {
            await svc.FulfillSingleReservationAsync(orderId, reservationId, HttpContext.RequestAborted);
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
    [Authorize(Roles = "Manager,WarehouseWorker, Administrator")]
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
    [Authorize(Roles = "Manager, Administrator")]
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
    [Authorize(Roles = "WarehouseWorker, Manager, Administrator")]
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

    [HttpGet("stocks")]
    [Authorize(Roles = "Manager, WarehouseWorker, Administrator")]
    public async Task<IActionResult> GetStocks([FromQuery] int? branchId, CancellationToken ct)
    {
        try
        {
            var stocks = await svc.GetStocksAsync(branchId, ct);
            return Ok(stocks);
        }
        catch (BranchNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unexpected error", details = ex.Message });
        }
    }
    
    [HttpPost("stocks/recalculate-thresholds")]
    [Authorize(Roles = "Manager, WarehouseWorker, Administrator")]
    public async Task<IActionResult> RecalculateThresholds(CancellationToken ct)
    {
        try
        {
            await svc.UpdateDynamicCriticalThresholdsAsync(ct);
            return Ok(new { message = "Critical thresholds recalculated successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Unexpected error", details = ex.Message });
        }
    }

    private ReservationDto ToResponse(StockReservation r)
    {
        var prio = _orderSvc.CalculatePriority(r.OrderItem.Order);

        return new ReservationDto()
        {
            ReservationId = r.Id,
            OrderId = r.OrderItem.OrderId,
            ProductName = r.ProductStock.Product?.Name,
            BranchId = r.ProductStock.BranchId,
            BranchName = r.ProductStock.Branch?.Name,
            Quantity = r.Quantity,
            Status = r.Status.ToString(),
            CreatedAt = r.CreatedAt,
            OrderStatus = r.OrderItem.Order.Status.ToString(),
            Priority = prio.ToString(),
            CustomerName = r.OrderItem.Order.CustomerName,
            ShippingAddress = r.OrderItem.Order.ShippingAddress
        };
    }
}
