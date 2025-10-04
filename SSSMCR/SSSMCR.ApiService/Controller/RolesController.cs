using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/roles")]
[Authorize (Roles = "Administrator")]
public class RolesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<object>> GetAll()
        => await db.Roles
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();
}