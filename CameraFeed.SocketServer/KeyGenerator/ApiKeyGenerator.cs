using CameraFeed.SocketServer.Data;
using System.Security.Cryptography;
using System.Text;

namespace CameraFeed.SocketServer.KeyGenerator;

public interface IApiKeyGenerator
{
    ApiKeyEntity GenerateKey(int userId, out string rawKey);
}

public class ApiKeyGenerator : IApiKeyGenerator
{
    public ApiKeyEntity GenerateKey(int userId, out string rawKey)
    {
        // 1. Generate random 32-byte key
        rawKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // 2. Hash before storing
        string hash = Convert.ToBase64String(
        SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));

        var apiKeyEntity = new ApiKeyEntity
        {
            KeyHash = hash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            UserId = userId
        };

        return apiKeyEntity;
    }
}
