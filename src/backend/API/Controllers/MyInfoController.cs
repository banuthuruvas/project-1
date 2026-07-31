using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Services.Services.MyInfo;
using Shared.Helpers;

namespace API.Controllers;

public class MyInfoController : BaseController
{
    private const string MyInfoStateCachePrefix = "myinfo:state:";
    private static readonly TimeSpan MyInfoStateLifetime = TimeSpan.FromMinutes(10);

    private readonly IMyInfoService _myInfoService;
    private readonly IDistributedCache _distributedCache;

    public MyInfoController(
        IMyInfoService myInfoService,
        IDistributedCache distributedCache)
    {
        _myInfoService = myInfoService;
        _distributedCache = distributedCache;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuthorizeUrl()
    {
        if (!_myInfoService.IsConfigured)
            return BadRequest(new { message = "MyInfo/Singpass is not configured" });

        var stateId = Guid.NewGuid().ToString("N");
        var issuedAtUtc = DateTimeHelper.UtcOffsetNow;
        var authorizationRequest = await _myInfoService.CreateAuthorizationRequestAsync(stateId);

        _distributedCache.SetString(
            GetStateCacheKey(stateId),
            JsonSerializer.Serialize(new MyInfoAuthSessionState(
                stateId,
                authorizationRequest.CodeVerifier,
                authorizationRequest.Nonce,
                authorizationRequest.DpopPrivateKey,
                issuedAtUtc)),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = MyInfoStateLifetime,
            });

        return Ok(new { authorizeUrl = authorizationRequest.AuthorizeUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Callback([FromBody] MyInfoCallbackRequest request)
    {
        if (!_myInfoService.IsConfigured)
            return BadRequest(new { message = "MyInfo/Singpass is not configured" });

        if (string.IsNullOrWhiteSpace(request.AuthCode) || string.IsNullOrWhiteSpace(request.State))
            return BadRequest(new { message = "Both authCode and state are required" });

        var cacheKey = GetStateCacheKey(request.State);
        var cachedSession = await _distributedCache.GetStringAsync(cacheKey);
        if (string.IsNullOrWhiteSpace(cachedSession))
            return BadRequest(new { message = "MyInfo state is invalid or has already been used" });

        var authSession = JsonSerializer.Deserialize<MyInfoAuthSessionState>(cachedSession);
        if (authSession is null || !string.Equals(authSession.StateId, request.State, StringComparison.Ordinal))
            return BadRequest(new { message = "MyInfo state is invalid or has already been used" });

        if (DateTimeHelper.UtcOffsetNow - authSession.IssuedAtUtc > MyInfoStateLifetime)
        {
            await _distributedCache.RemoveAsync(cacheKey);
            return BadRequest(new { message = "MyInfo state has expired" });
        }

        await _distributedCache.RemoveAsync(cacheKey);

        var personData = await _myInfoService.GetPersonDataAsync(
            request.AuthCode,
            authSession.CodeVerifier,
            authSession.Nonce,
            authSession.DpopPrivateKey);

        return Ok(personData);
    }

    [HttpGet]
    public IActionResult IsConfigured()
    {
        return Ok(new { configured = _myInfoService.IsConfigured });
    }

    private static string GetStateCacheKey(string stateId)
    {
        return $"{MyInfoStateCachePrefix}{stateId}";
    }
}

public record MyInfoCallbackRequest(string AuthCode, string State);

public record MyInfoAuthSessionState(
    string StateId,
    string CodeVerifier,
    string Nonce,
    string DpopPrivateKey,
    DateTimeOffset IssuedAtUtc);
