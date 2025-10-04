namespace SSSMCR.ApiService.Model;

public class Company
{
    public int Id { get; set; }
    public string CompanyName { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string PostalCode { get; set; }
    public string TaxIdentificationNumber { get; set; }
    public string BankAccountNumber { get; set; }
    public string ContactEmail { get; set; }
    public string ContactPhone { get; set; }
}