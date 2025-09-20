using CameraFeed.SocketServer.KeyGenerator;
using CameraFeed.SocketServer.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CameraFeed.SocketServer.Controllers;

[ApiController]
[Route("[controller]")]
public class ApiKeyController(IApiKeyGenerator apiKeyGenerator, IApiKeyRepository apiKeyRepository) : ControllerBase
{
    private readonly IApiKeyGenerator _apiKeyGenerator = apiKeyGenerator;
    private readonly IApiKeyRepository _apiKeyRepository = apiKeyRepository;

    [Authorize]
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateKey()
    {
        //TODO: Get the user id here from token and store it in the apiKeyEntity
        var apiKeyEntity = _apiKeyGenerator.GenerateKey(userId: 1, out string rawKey); // Placeholder, set the actual user ID when creating the key

        await _apiKeyRepository.AddAsync(apiKeyEntity);
        await _apiKeyRepository.SaveChangesAsync();

        // 3. Return plaintext ONCE
        return Ok(new { apiKey = rawKey });
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteKey(int id)
    {
        bool deleted = await _apiKeyRepository.DeleteAsync(id);
        if (deleted)
        {
            await _apiKeyRepository.SaveChangesAsync();
            return Ok(new { id, deleted });
        }
        return NotFound();
    }
}
