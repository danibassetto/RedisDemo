using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using RedisDemo.Arguments;
using StackExchange.Redis;
using System.Text;

namespace RedisDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class CacheController(IDistributedCache cache, IConnectionMultiplexer redis) : ControllerBase
{
    private readonly IDistributedCache _cache = cache;
    private readonly IConnectionMultiplexer _redis = redis;

    /// <summary>
    /// Adiciona um item ao cache com um tempo de expiração especificado.
    /// </summary>
    /// <param name="input">Objeto contendo a chave, valor e tempo de expiração em segundos.</param>
    /// <returns>Mensagem de sucesso ou erro.</returns>
    /// <response code="200">Cache adicionado com sucesso.</response>
    /// <response code="500">Erro ao adicionar cache.</response>
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Set(InputSetCache input)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(input.SecondExpiration)
            };
            await _cache.SetAsync(input.Key, Encoding.UTF8.GetBytes(input.Value), options);
            return Ok("Cache adicionado com sucesso!");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erro ao adicionar cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtém um item do cache e seu ttl com base na chave fornecida.
    /// </summary>
    /// <param name="key">A chave do item a ser recuperado.</param>
    /// <returns>Valor do cache e o tempo de expiração restante ou mensagem de erro.</returns>
    /// <response code="200">Valor do cache e tempo de expiração restante.</response>
    /// <response code="404">Cache não encontrado.</response>
    /// <response code="500">Erro ao recuperar cache.</response>
    [HttpGet("{key}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetByKey(string key)
    {
        try
        {
            var value = await _cache.GetAsync(key);
            if (value != null)
            {
                var ttl = await GetTimeToLive(key);
                var result = new
                {
                    Value = Encoding.UTF8.GetString(value),
                    TimeToLive = ttl
                };
                return Ok(result);
            }

            return NotFound("Cache não encontrado!");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erro ao recuperar cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Exclui um item do cache com base na chave fornecida.
    /// </summary>
    /// <param name="key">A chave do item a ser excluído.</param>
    /// <returns>Mensagem de sucesso ou erro.</returns>
    /// <response code="200">Cache excluído.</response>
    /// <response code="500">Erro ao excluir cache.</response>
    [HttpDelete("{key}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteByKey(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            return Ok("Cache excluído");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erro ao excluir cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Limpa todo o cache.
    /// </summary>
    /// <returns>Mensagem de sucesso ou erro.</returns>
    /// <response code="200">Todo o cache foi apagado.</response>
    /// <response code="500">Erro ao limpar cache.</response>
    [HttpDelete]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Clear()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var db = _redis.GetDatabase();

            // Itera sobre as chaves e remove cada uma delas
            var keys = server.Keys();
            foreach (var key in keys)
            {
                await db.KeyDeleteAsync(key);
            }

            return Ok("Todo o cache foi apagado.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erro ao limpar cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtém todas as chaves do cache.
    /// </summary>
    /// <returns>Lista de chaves ou mensagem de erro.</returns>
    /// <response code="200">Lista de chaves.</response>
    /// <response code="500">Erro ao recuperar chaves.</response>
    [HttpGet("GetAllKey")]
    [ProducesResponseType(200)]
    [ProducesResponseType(500)]
    public IActionResult GetAllKey()
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys();
            var keyList = keys.Select(k => k.ToString()).ToList();
            return Ok(keyList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erro ao recuperar chaves: {ex.Message}");
        }
    }

    private async Task<TimeSpan?> GetTimeToLive(string key)
    {
        var db = _redis.GetDatabase();
        var ttl = await db.KeyTimeToLiveAsync(key);
        return ttl;
    }
}