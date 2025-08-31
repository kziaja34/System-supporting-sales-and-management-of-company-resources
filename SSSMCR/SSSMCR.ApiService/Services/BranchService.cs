using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services.Interfaces;

namespace SSSMCR.ApiService.Services;

public class BranchService : GenericService<Branch>, IBranchService
{
    public BranchService(AppDbContext context) : base(context) { }
}