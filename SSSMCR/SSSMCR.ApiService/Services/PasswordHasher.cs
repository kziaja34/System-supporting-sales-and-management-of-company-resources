
namespace SSSMCR.ApiService.Services;


public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verify(string hash, string password) => BCrypt.Net.BCrypt.Verify(password, hash);
}