using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Model.Exceptions;
using SSSMCR.ApiService.Services;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/warehouse")]
[Authorize]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _svc;

    public WarehouseController(IWarehouseService svc) => _svc = svc;

    [HttpPost("orders/{orderId}/reserve")]
    [Authorize(Roles = "Manager,Seller")]
    public async Task<IActionResult> Reserve(int orderId, [FromBody] int? preferredBranchId)
    {
        try
        {
            var result = await _svc.ReserveForOrderAsync(orderId, preferredBranchId);
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
            await _svc.FulfillReservationsAsync(orderId);
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
            await _svc.FulfillReservationForBranchAsync(orderId, branchId, HttpContext.RequestAborted);
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
    [Authorize(Roles = "Manager, Seller")]
    public async Task<IActionResult> Release(int orderId)
    {
        try
        {
            await _svc.ReleaseReservationsForOrderAsync(orderId);
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
}