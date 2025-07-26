namespace CameraFeed.Web.Services;

public interface IAllowedUsersService
{
    IEnumerable<string> GetAllowedUsers();
    bool IsAllowed(string accountId);
    void AddUser(string accountId);
    void RemoveUser(string accountId);
}

public class AllowedUsersService : IAllowedUsersService
{
    private readonly HashSet<string> _allowedUsers = ["Katalyst Krueger"];

    public IEnumerable<string> GetAllowedUsers() => _allowedUsers;

    public bool IsAllowed(string accountId) => _allowedUsers.Contains(accountId);

    public void AddUser(string accountId) => _allowedUsers.Add(accountId);

    public void RemoveUser(string accountId) => _allowedUsers.Remove(accountId);
}