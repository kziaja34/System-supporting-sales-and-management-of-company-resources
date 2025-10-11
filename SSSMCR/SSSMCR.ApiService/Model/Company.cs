using System.ComponentModel.DataAnnotations;

namespace SSSMCR.ApiService.Model;

public class Company
{
    public int Id { get; set; }
    [MaxLength(100)]
    public string? CompanyName { get; set; }
    [MaxLength(100)]
    public string? Address { get; set; }
    [MaxLength(100)]
    public string? City { get; set; }
    [MaxLength(100)]
    public string? PostalCode { get; set; }
    [MaxLength(100)]
    public string? TaxIdentificationNumber { get; set; }
    [MaxLength(100)]
    public string? BankAccountNumber { get; set; }
    [MaxLength(100)]
    public string? ContactEmail { get; set; }
    [MaxLength(100)]
    public string? ContactPhone { get; set; }
}