using System.Security.Claims;

namespace SmartBabySitter.Services;

public interface ICurrentUser
{
    int UserId { get; }
    bool IsAuthenticated { get; }
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http)
    {
        _http = http;
    }

    public bool IsAuthenticated => _http.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public int UserId
    {
        get
        {
            var id = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var n) ? n : 0;
        }
    }
}