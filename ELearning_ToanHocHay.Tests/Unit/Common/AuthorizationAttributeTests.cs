using System.Security.Claims;
using ELearning_ToanHocHay_Control.Attributes;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace ELearning_ToanHocHay.Tests.Unit.Common;

/// <summary>§3.36 — UT-ATTR-*.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class AuthorizationAttributeTests
{
    private static AuthorizationFilterContext Ctx(ClaimsPrincipal user, IServiceProvider? services = null)
    {
        var http = new DefaultHttpContext { User = user };
        if (services != null) http.RequestServices = services;
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static int? Status(AuthorizationFilterContext ctx) => (ctx.Result as ObjectResult)?.StatusCode;

    // ---- AuthorizeUserType ----

    [Fact] // UT-ATTR-01
    public void AuthorizeUserType_allows_a_permitted_role()
    {
        var ctx = Ctx(Claims.For(UserType.FinanceManager, userId: 1));
        new AuthorizeUserTypeAttribute(UserType.FinanceManager, UserType.SystemAdmin).OnAuthorization(ctx);
        ctx.Result.Should().BeNull();
    }

    [Fact] // UT-ATTR-02
    public void AuthorizeUserType_forbids_a_non_permitted_role()
    {
        var ctx = Ctx(Claims.For(UserType.Student, userId: 1));
        new AuthorizeUserTypeAttribute(UserType.FinanceManager, UserType.SystemAdmin).OnAuthorization(ctx);
        Status(ctx).Should().Be(403);
    }

    [Fact] // UT-ATTR-03
    public void AuthorizeUserType_401_when_not_authenticated()
    {
        var ctx = Ctx(Claims.Anonymous());
        new AuthorizeUserTypeAttribute(UserType.FinanceManager).OnAuthorization(ctx);
        Status(ctx).Should().Be(401);
    }

    // ---- AuthorizeContentRole ----

    [Theory] // UT-ATTR-04
    [InlineData(UserType.ContentEditor)]
    [InlineData(UserType.AcademicReviewer)]
    [InlineData(UserType.SystemAdmin)]
    public void AuthorizeContentRole_allows_content_roles(UserType type)
    {
        var ctx = Ctx(Claims.For(type, userId: 1));
        new AuthorizeContentRoleAttribute().OnAuthorization(ctx);
        ctx.Result.Should().BeNull();
    }

    [Theory] // UT-ATTR-05
    [InlineData(UserType.Student)]
    [InlineData(UserType.Parent)]
    public void AuthorizeContentRole_forbids_students_and_parents(UserType type)
    {
        var ctx = Ctx(Claims.For(type, userId: 1));
        new AuthorizeContentRoleAttribute().OnAuthorization(ctx);
        Status(ctx).Should().Be(403);
    }

    // ---- SePayApiKey ----

    private static IServiceProvider SePayServices(string validKey = "good-key")
    {
        var sepay = Substitute.For<ISePayService>();
        sepay.ValidateApiKey(validKey).Returns(true);
        return new ServiceCollection().AddSingleton(sepay).AddLogging().BuildServiceProvider();
    }

    [Fact] // UT-ATTR-06
    public void SePayApiKey_allows_a_valid_key()
    {
        var ctx = Ctx(Claims.Anonymous(), SePayServices());
        ctx.HttpContext.Request.Headers["Authorization"] = "Apikey good-key";
        new SePayApiKeyAttribute().OnAuthorization(ctx);
        ctx.Result.Should().BeNull();
    }

    [Fact] // UT-ATTR-07
    public void SePayApiKey_401_when_key_wrong_or_missing()
    {
        var missing = Ctx(Claims.Anonymous(), SePayServices());
        new SePayApiKeyAttribute().OnAuthorization(missing);
        (missing.Result as ObjectResult)!.StatusCode.Should().Be(401);

        var wrong = Ctx(Claims.Anonymous(), SePayServices());
        wrong.HttpContext.Request.Headers["Authorization"] = "Apikey nope";
        new SePayApiKeyAttribute().OnAuthorization(wrong);
        (wrong.Result as ObjectResult)!.StatusCode.Should().Be(401);
    }

    [Fact] // UT-ATTR-08
    public void SePayApiKey_401_on_wrong_scheme()
    {
        var ctx = Ctx(Claims.Anonymous(), SePayServices());
        ctx.HttpContext.Request.Headers["Authorization"] = "Bearer good-key";
        new SePayApiKeyAttribute().OnAuthorization(ctx);
        (ctx.Result as ObjectResult)!.StatusCode.Should().Be(401);
    }
}
