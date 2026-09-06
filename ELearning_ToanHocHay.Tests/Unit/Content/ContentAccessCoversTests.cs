using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Implementations;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Content;

/// <summary>§3.24 — UT-GATE-COVERS-*. <c>internal static ContentAccessService.Covers</c>.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class ContentAccessCoversTests
{
    private static Course Course(int subjectId = 1, int gradeId = 6)
        => new() { CourseId = 100, SubjectId = subjectId, GradeLevelId = gradeId, Title = "c", Slug = "c" };

    private static PackageEntitlement Ent(EntitlementScope scope, int? subjectId = null, int? gradeId = null, int? courseId = null)
        => new() { ScopeType = scope, SubjectId = subjectId, GradeLevelId = gradeId, CourseId = courseId };

    [Fact] // UT-GATE-COVERS-01
    public void AllContent_covers_any_course()
        => ContentAccessService.Covers(Ent(EntitlementScope.AllContent), Course()).Should().BeTrue();

    [Theory] // UT-GATE-COVERS-02 / 03
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void Subject_scope(int entSubjectId, bool expected)
        => ContentAccessService.Covers(Ent(EntitlementScope.Subject, subjectId: entSubjectId), Course(subjectId: 1))
            .Should().Be(expected);

    [Theory] // UT-GATE-COVERS-04 / 05
    [InlineData(6, true)]
    [InlineData(7, false)]
    public void Grade_scope(int entGradeId, bool expected)
        => ContentAccessService.Covers(Ent(EntitlementScope.Grade, gradeId: entGradeId), Course(gradeId: 6))
            .Should().Be(expected);

    [Theory] // UT-GATE-COVERS-06 / 07 / 08
    [InlineData(1, 6, true)]
    [InlineData(1, 7, false)]
    [InlineData(2, 6, false)]
    public void SubjectGrade_scope(int entSubjectId, int entGradeId, bool expected)
        => ContentAccessService.Covers(
                Ent(EntitlementScope.SubjectGrade, subjectId: entSubjectId, gradeId: entGradeId),
                Course(subjectId: 1, gradeId: 6))
            .Should().Be(expected);
}
