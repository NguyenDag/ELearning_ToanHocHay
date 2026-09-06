using System.Net;
using System.Net.Http.Json;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ELearning_ToanHocHay.Tests.Integration;

/// <summary>§4 F6 — Dashboard &amp; liên kết phụ huynh (IT-F6).</summary>
[Collection(IntegrationCollection.Name)]
[Trait("Level", "Integration")]
[Trait("Flow", "Parent")]
public class IT_F6_ParentTests : IntegrationTest
{
    public IT_F6_ParentTests(ApiFactory app) : base(app) { }

    [SkippableFact] // IT-F6-01
    public async Task IT_F6_01_Parent_creates_an_invite()
    {
        RequireDocker();
        var (userId, parentId, _) = await Flow.NewParentAsync();

        var res = await App.As(userId).PostAsJsonAsync($"/api/parents/{parentId}/invites",
            new { InviteeEmail = "con@flow.test", Relationship = "Father", ExpiresInDays = 7 });

        await res.ShouldBeOk();
        await App.Db(async db => (await db.ParentInvites.AnyAsync(i => i.ParentId == parentId)).Should().BeTrue());
    }

    [SkippableFact] // IT-F6-02
    public async Task IT_F6_02_Student_links_by_connection_code()
    {
        RequireDocker();
        var (_, parentId, code) = await Flow.NewParentAsync();
        var (studentUserId, studentId) = await Flow.NewStudentAsync();

        var res = await App.As(studentUserId).PostAsJsonAsync("/api/parents/link",
            new { Code = code, Relationship = "Mother" });

        await res.ShouldBeOk();
        await App.Db(async db =>
            (await db.ParentLinks.SingleAsync(l => l.ParentId == parentId && l.StudentId == studentId))
                .Status.Should().Be(LinkStatus.Active));
    }

    [SkippableFact] // IT-F6-03
    public async Task IT_F6_03_Bad_connection_code_is_rejected()
    {
        RequireDocker();
        var (studentUserId, _) = await Flow.NewStudentAsync();

        var res = await App.As(studentUserId).PostAsJsonAsync("/api/parents/link", new { Code = "ZZZZZZZZ" });

        res.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [SkippableFact] // IT-F6-04
    public async Task IT_F6_04_Parent_lists_children_and_overview()
    {
        RequireDocker();
        var (parentUserId, parentId, code) = await Flow.NewParentAsync();
        var (studentUserId, _) = await Flow.NewStudentAsync();
        await App.As(studentUserId).PostAsJsonAsync("/api/parents/link", new { Code = code });
        var client = App.As(parentUserId);

        await (await client.GetAsync($"/api/parents/{parentId}/children")).ShouldBeOk();
        await (await client.GetAsync($"/api/parents/{parentId}/children/overview")).ShouldBeOk();
    }

    [SkippableFact] // IT-F6-05
    public async Task IT_F6_05_Linked_parent_can_read_child_history_and_dashboard()
    {
        RequireDocker();
        var parent = App.AsRole(TestRole.ParentLinked);

        (await parent.GetAsync($"/api/exercise-attempts/student/{Ids.StudentAId}/history"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await parent.GetAsync($"/api/students/{Ids.StudentAId}/dashboard/overview"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact] // IT-F6-06
    public async Task IT_F6_06_Unlinked_parent_is_forbidden()
    {
        RequireDocker();
        var stranger = App.AsRole(TestRole.ParentStranger);

        (await stranger.GetAsync($"/api/exercise-attempts/student/{Ids.StudentAId}/history"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await stranger.GetAsync($"/api/students/{Ids.StudentAId}/dashboard/overview"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F6-07
    public async Task IT_F6_07_Revoking_a_link_drops_dashboard_access()
    {
        RequireDocker();
        var (parentUserId, parentId, code) = await Flow.NewParentAsync();
        var (studentUserId, studentId) = await Flow.NewStudentAsync();
        await App.As(studentUserId).PostAsJsonAsync("/api/parents/link", new { Code = code });
        var parent = App.As(parentUserId);

        (await parent.GetAsync($"/api/students/{studentId}/dashboard/overview")).StatusCode.Should().Be(HttpStatusCode.OK);

        await (await parent.DeleteAsync($"/api/parents/{parentId}/children/{studentId}")).ShouldBeOk();

        (await parent.GetAsync($"/api/students/{studentId}/dashboard/overview")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await parent.GetAsync($"/api/exercise-attempts/student/{studentId}/history")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact] // IT-F6-08
    public async Task IT_F6_08_Parent_cannot_edit_another_parent_and_delete_needs_admin()
    {
        RequireDocker();
        var (userA, _, _) = await Flow.NewParentAsync();
        var (_, parentIdB, _) = await Flow.NewParentAsync();

        (await App.As(userA).PutAsJsonAsync($"/api/parents/{parentIdB}", new { Job = "hack" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await App.AsRole(TestRole.StudentA).DeleteAsync($"/api/parents/{parentIdB}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
