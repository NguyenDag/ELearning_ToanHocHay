using System.Security.Claims;
using ELearning_ToanHocHay_Control.Common;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay.Tests.Unit.Infrastructure;

/// <summary>
/// Dựng <see cref="ClaimsPrincipal"/> mang đúng custom claim (<see cref="CustomJwtClaims"/>)
/// mà <c>ClaimsPrincipalExtensions</c> và các attribute phân quyền đọc — §3.12 / §3.36.
/// </summary>
public static class Claims
{
    public static ClaimsPrincipal For(
        UserType type,
        int? userId = null,
        int? studentId = null,
        int? parentId = null,
        string? email = null)
    {
        var list = new List<Claim>
        {
            new(CustomJwtClaims.UserType, type.ToString()),
            new(ClaimTypes.Role, type.ToString()),
        };

        if (userId is { } uid)
        {
            list.Add(new Claim(CustomJwtClaims.UserId, uid.ToString()));
            list.Add(new Claim(ClaimTypes.NameIdentifier, uid.ToString()));
        }
        if (studentId is { } sid) list.Add(new Claim(CustomJwtClaims.StudentId, sid.ToString()));
        if (parentId is { } pid) list.Add(new Claim(CustomJwtClaims.ParentId, pid.ToString()));
        if (email is not null) list.Add(new Claim(ClaimTypes.Email, email));

        return new ClaimsPrincipal(new ClaimsIdentity(list, authenticationType: "TestAuth"));
    }

    /// <summary>Khách chưa đăng nhập — identity không xác thực, không claim.</summary>
    public static ClaimsPrincipal Anonymous()
        => new(new ClaimsIdentity());
}
