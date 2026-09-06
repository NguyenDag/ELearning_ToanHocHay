using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace ELearning_ToanHocHay.Tests.Unit.Content;

/// <summary>§3.25 — UT-GATE-*. Fake <see cref="IEnrollmentRepository"/> + ClaimsPrincipal dựng tay.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class ContentAccessServiceTests
{
    private const int StudentId = 5;
    private const int CourseId = 100;

    private readonly IEnrollmentRepository _enrol = Substitute.For<IEnrollmentRepository>();

    private ContentAccessService Svc() => new(_enrol);

    private static Course Course(CourseStatus status, int subjectId = 1, int gradeId = 6)
        => new() { CourseId = CourseId, SubjectId = subjectId, GradeLevelId = gradeId, Title = "c", Slug = "c", Status = status };

    private static PackageEntitlement Ent(EntitlementScope scope, int? subjectId = null, int? gradeId = null)
        => new() { ScopeType = scope, SubjectId = subjectId, GradeLevelId = gradeId };

    private void NoEntitlements() => _enrol.GetActiveEntitlementsAsync(StudentId).Returns(new List<PackageEntitlement>());
    private void NoEnrolment() => _enrol.GetActiveEnrolmentAsync(StudentId, CourseId).Returns((StudentCourse?)null);

    [Fact] // UT-GATE-01
    public async Task Draft_visible_to_editor()
        => (await Svc().GetCourseAccessAsync(Claims.For(UserType.ContentEditor, userId: 1), Course(CourseStatus.Draft)))
            .Should().Be(ContentAccessLevel.Full);

    [Fact] // UT-GATE-02
    public async Task Draft_hidden_from_student()
    {
        NoEnrolment();
        NoEntitlements();
        (await Svc().GetCourseAccessAsync(Claims.For(UserType.Student, userId: 1, studentId: StudentId), Course(CourseStatus.Draft)))
            .Should().Be(ContentAccessLevel.None);
    }

    [Fact] // UT-GATE-03
    public async Task Draft_hidden_from_anonymous()
        => (await Svc().GetCourseAccessAsync(Claims.Anonymous(), Course(CourseStatus.Draft)))
            .Should().Be(ContentAccessLevel.None);

    [Theory] // UT-GATE-04
    [InlineData(UserType.ContentEditor)]
    [InlineData(UserType.AcademicReviewer)]
    [InlineData(UserType.SystemAdmin)]
    public async Task Published_full_for_staff(UserType type)
        => (await Svc().GetCourseAccessAsync(Claims.For(type, userId: 1), Course(CourseStatus.Published)))
            .Should().Be(ContentAccessLevel.Full);

    [Fact] // UT-GATE-05
    public async Task Published_free_only_for_anonymous()
        => (await Svc().GetCourseAccessAsync(Claims.Anonymous(), Course(CourseStatus.Published)))
            .Should().Be(ContentAccessLevel.FreeOnly);

    [Fact] // UT-GATE-06
    public async Task Published_free_only_for_parent()
        => (await Svc().GetCourseAccessAsync(Claims.For(UserType.Parent, userId: 1, parentId: 2), Course(CourseStatus.Published)))
            .Should().Be(ContentAccessLevel.FreeOnly);

    [Fact] // UT-GATE-07
    public async Task Published_full_for_enrolled_student()
    {
        _enrol.GetActiveEnrolmentAsync(StudentId, CourseId).Returns(new StudentCourse { StudentCourseId = 1 });

        (await Svc().GetCourseAccessAsync(Claims.For(UserType.Student, userId: 1, studentId: StudentId), Course(CourseStatus.Published)))
            .Should().Be(ContentAccessLevel.Full);
    }

    [Fact] // UT-GATE-08
    public async Task Published_full_for_matching_subject_grade_entitlement()
    {
        NoEnrolment();
        _enrol.GetActiveEntitlementsAsync(StudentId)
            .Returns(new List<PackageEntitlement> { Ent(EntitlementScope.SubjectGrade, subjectId: 1, gradeId: 6) });

        (await Svc().GetCourseAccessAsync(Claims.For(UserType.Student, userId: 1, studentId: StudentId),
                Course(CourseStatus.Published, subjectId: 1, gradeId: 6)))
            .Should().Be(ContentAccessLevel.Full);
    }

    [Fact] // UT-GATE-09
    public async Task Published_full_for_all_content_entitlement()
    {
        NoEnrolment();
        _enrol.GetActiveEntitlementsAsync(StudentId)
            .Returns(new List<PackageEntitlement> { Ent(EntitlementScope.AllContent) });

        (await Svc().GetCourseAccessAsync(Claims.For(UserType.Student, userId: 1, studentId: StudentId), Course(CourseStatus.Published)))
            .Should().Be(ContentAccessLevel.Full);
    }

    [Fact] // UT-GATE-10
    public async Task Published_free_only_when_entitlement_is_for_another_subject()
    {
        NoEnrolment();
        _enrol.GetActiveEntitlementsAsync(StudentId)
            .Returns(new List<PackageEntitlement> { Ent(EntitlementScope.Subject, subjectId: 999) });

        (await Svc().GetCourseAccessAsync(Claims.For(UserType.Student, userId: 1, studentId: StudentId),
                Course(CourseStatus.Published, subjectId: 1)))
            .Should().Be(ContentAccessLevel.FreeOnly);
    }

    [Fact] // UT-GATE-11
    public async Task Published_free_only_when_entitlement_expired_repo_returns_none()
    {
        NoEnrolment();
        NoEntitlements();

        (await Svc().GetCourseAccessAsync(Claims.For(UserType.Student, userId: 1, studentId: StudentId), Course(CourseStatus.Published)))
            .Should().Be(ContentAccessLevel.FreeOnly);
    }
}
