var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/api/sso/exchange", (ExchangeRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.exchangeToken))
    {
        return Results.BadRequest(new
        {
            isAuthenticated = false,
            errorMessage = "exchangeToken is required."
        });
    }

    var userId = ResolveUserId(request);
    var userName = !string.IsNullOrWhiteSpace(request.username) ? request.username : userId;
    var email = !string.IsNullOrWhiteSpace(request.email) ? request.email : $"{userId}@nie.edu.sg";
    var fullName = !string.IsNullOrWhiteSpace(request.username)
        ? request.username
        : userId;

    return Results.Ok(new
    {
        userId,
        firstName = userName,
        lastName = string.Empty,
        fullName,
        userName,
        department = "Portal SSO",
        email,
        sessionToken = Guid.NewGuid().ToString("N"),
        userType = "PortalSso",
        isAuthenticated = true,
        errorMessage = string.Empty
    });
});

app.Run();

static string ResolveUserId(ExchangeRequest request)
{
    if (!string.IsNullOrWhiteSpace(request.subject))
        return request.subject;

    if (!string.IsNullOrWhiteSpace(request.username))
        return request.username;

    if (!string.IsNullOrWhiteSpace(request.email))
        return request.email.Split('@', 2, StringSplitOptions.TrimEntries)[0];

    return "portal.user";
}

internal sealed record ExchangeRequest(
    string? state,
    string? exchangeToken,
    string? sourceSystemId,
    string? sourceUrl,
    string? username,
    string? email,
    string? subject);
