using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services;

public interface ICompanyService : IGenericService<Company>
{
    Task<Company?> GetCompanyAsync();
    Task<Company> UpdateCompanyAsync(Company updatedCompany);
}


public class CompanyService(AppDbContext context) : GenericService<Company>(context), ICompanyService
{
    public async Task<Company?> GetCompanyAsync()
    {
        return await _context.Companies.FirstOrDefaultAsync();
    }

    public async Task<Company> UpdateCompanyAsync(Company updatedCompany)
    {
        var existing = await _context.Companies.FirstOrDefaultAsync();

        if (existing == null)
        {
            _context.Companies.Add(updatedCompany);
        }
        else
        {
            existing.CompanyName = updatedCompany.CompanyName;
            existing.Address = updatedCompany.Address;
            existing.City = updatedCompany.City;
            existing.PostalCode = updatedCompany.PostalCode;
            existing.TaxIdentificationNumber = updatedCompany.TaxIdentificationNumber;
            existing.BankAccountNumber = updatedCompany.BankAccountNumber;
            existing.ContactEmail = updatedCompany.ContactEmail;
            existing.ContactPhone = updatedCompany.ContactPhone;
        }

        await _context.SaveChangesAsync();
        return existing ?? updatedCompany;
    }
}