using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/company")]
[Authorize]
public class CompanyController(ICompanyService service) : ControllerBase
{
    private readonly ICompanyService _service = service;
    
    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<CompanyResponse>> GetCompany()
    {
        var entity = await _service.GetCompanyAsync();
        if (entity == null)
            return NotFound();

        return Ok(ToResponse(entity));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<CompanyResponse>> UpdateCompany(int id, CompanyRequest dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var entity = ToEntity(dto);
            var saved = await _service.UpdateCompanyAsync(entity);
            return Ok(ToResponse(saved));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }



    private CompanyResponse ToResponse(Company entity)
    {
        return new CompanyResponse
        {
            Id = entity.Id,
            CompanyName = entity.CompanyName,
            Address = entity.Address,
            City = entity.City,
            PostalCode = entity.PostalCode,
            TaxIdentificationNumber = entity.TaxIdentificationNumber,
            BankAccountNumber = entity.BankAccountNumber,
            ContactEmail = entity.ContactEmail,
            ContactPhone = entity.ContactPhone
        };
    }

    private Company ToEntity(CompanyRequest dto)
    {
        return new Company
        {
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            PostalCode = dto.PostalCode,
            TaxIdentificationNumber = dto.TaxIdentificationNumber,
            BankAccountNumber = dto.BankAccountNumber,
            ContactEmail = dto.ContactEmail,
            ContactPhone = dto.ContactPhone
        };
    }
}