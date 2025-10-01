namespace SSSMCR.ApiService.Model.Exceptions;

public class BranchNotFoundException : Exception
{
    public BranchNotFoundException(int branchId)
        : base($"Branch with id {branchId} was not found.") { }
}