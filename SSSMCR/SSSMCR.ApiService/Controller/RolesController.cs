using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;

[ApiController]
[Route("api/roles")]
[Authorize (Roles = "Administrator")]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _db;
    public RolesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IEnumerable<object>> GetAll()
        => await _db.Roles
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();
}