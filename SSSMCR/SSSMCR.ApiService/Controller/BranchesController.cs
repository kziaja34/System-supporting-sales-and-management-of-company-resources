using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;

[ApiController]
[Route("api/branches")]
public class BranchesController : ControllerBase
{
    private readonly AppDbContext _db;
    public BranchesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IEnumerable<object>> GetAll()
        => await _db.Branches
            .Select(b => new { b.Id, b.Name })
            .ToListAsync();
}