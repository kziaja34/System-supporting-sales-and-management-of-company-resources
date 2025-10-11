namespace SSSMCR.ApiService.Model.Exceptions;

public class CurrentUserException : Exception
{
    public CurrentUserException(int id)
        : base($"You cannot edit privileges of your own account (id {id}) nor delete it.") { }
    public CurrentUserException(int id, string message)
        : base($"You cannot edit privileges of your own account (id {id}) nor delete it. {message}") { }
}