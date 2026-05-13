using Microsoft.AspNetCore.Authorization;

namespace TrueCapture.Shared.Authorization;

/// <summary>
/// Gate for `/api/admin/*` endpoints. Composes <see cref="AuthorizeAttribute"/>
/// against the named policy <c>"admin"</c> — the policy provider grants this
/// only when the JWT carries the <c>role=Admin</c> claim issued by
/// <c>TokenService.Issue</c> for users with <c>User.IsAdmin = true</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AdminOnlyAttribute : AuthorizeAttribute
{
    public AdminOnlyAttribute() : base("admin") { }
}
