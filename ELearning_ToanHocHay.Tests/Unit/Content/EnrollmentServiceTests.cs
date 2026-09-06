using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Repositories.Interfaces;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace ELearning_ToanHocHay.Tests.Unit.Content;

/// <summary>§3.26 — UT-ENROL-*. Repo + gate fake (NSubstitute).</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class EnrollmentServiceTests
{
    private const int StudentId = 42;
    private const int CourseId = 50;

    private readonly IEnrollmentRepository _repo = Substitute.For<IEnrollmentRepository>();
    private readonly ICourseRepository _courseRepo = Substitute.For<ICourseRepository>();
    private readonly IContentAccessService _access = Substitute.For<IContentAccessService>();

    private EnrollmentService Svc() => new(_repo, _courseRepo, _access);

    private Course ArrangeCourse(CourseStatus status = CourseStatus.Published, VersionState? versionState = VersionState.Published)
    {
        var course = Entities.NewCourse(status, tweak: c =>
        {
            c.CourseId = CourseId;
            c.Versions = versionState is null
                ? new List<CourseVersion>()
                : new List<CourseVersion> { new() { CourseVersionId = 9, CourseId = CourseId, State = versionState.Value } };
        });
        _courseRepo.GetCourseAsync(CourseId, Arg.Any<bool>()).Returns(course);
        return course;
    }

    private StudentCourse CaptureNewEnrolment()
    {
        StudentCourse? added = null;
        _repo.AddEnrolmentAsync(Arg.Do<StudentCourse>(sc => { sc.StudentCourseId = 123; added = sc; }))
            .Returns(ci => ci.Arg<StudentCourse>());
        _repo.GetEnrolmentsAsync(StudentId).Returns(_ => new List<StudentCourse> { added! });
        return null!;
    }

    [Fact] // UT-ENROL-01
    public async Task Missing_course()
    {
        _courseRepo.GetCourseAsync(CourseId, Arg.Any<bool>()).Returns((Course?)null);
        (await Svc().EnrollAsync(StudentId, CourseId)).Message.Should().Be("Không tìm thấy khoá học");
    }

    [Fact] // UT-ENROL-02
    public async Task Unpublished_course()
    {
        ArrangeCourse(status: CourseStatus.Draft);
        (await Svc().EnrollAsync(StudentId, CourseId)).Message.Should().Be("Khoá học chưa được xuất bản");
    }

    [Fact] // UT-ENROL-03
    public async Task Course_without_published_version()
    {
        ArrangeCourse(versionState: VersionState.InReview);
        (await Svc().EnrollAsync(StudentId, CourseId)).Message.Should().Be("Khoá học chưa có phiên bản xuất bản");
    }

    [Fact] // UT-ENROL-04
    public async Task Already_enrolled_returns_existing_without_inserting()
    {
        ArrangeCourse();
        _repo.GetActiveEnrolmentAsync(StudentId, CourseId)
            .Returns(new StudentCourse { StudentCourseId = 1, CourseId = CourseId });

        var res = await Svc().EnrollAsync(StudentId, CourseId);

        res.Success.Should().BeTrue();
        res.Message.Should().Be("Already enrolled");
        await _repo.DidNotReceive().AddEnrolmentAsync(Arg.Any<StudentCourse>());
    }

    [Fact] // UT-ENROL-05
    public async Task New_valid_enrolment_is_created()
    {
        ArrangeCourse();
        _repo.GetActiveEnrolmentAsync(StudentId, CourseId).Returns((StudentCourse?)null);
        _access.HasCourseEntitlementAsync(StudentId, Arg.Any<Course>()).Returns(false);
        CaptureNewEnrolment();

        var res = await Svc().EnrollAsync(StudentId, CourseId);

        res.Success.Should().BeTrue();
        res.Data.CourseId.Should().Be(CourseId);
        await _repo.Received().AddEnrolmentAsync(Arg.Is<StudentCourse>(sc =>
            sc.StudentId == StudentId && sc.CourseVersionId == 9 && sc.Source == EnrollSource.Self));
    }

    [Fact] // UT-ENROL-06
    public async Task Enrolment_with_entitlement_still_creates_record_as_subscription_source()
    {
        ArrangeCourse();
        _repo.GetActiveEnrolmentAsync(StudentId, CourseId).Returns((StudentCourse?)null);
        _access.HasCourseEntitlementAsync(StudentId, Arg.Any<Course>()).Returns(true);
        CaptureNewEnrolment();

        var res = await Svc().EnrollAsync(StudentId, CourseId);

        res.Success.Should().BeTrue();
        await _repo.Received().AddEnrolmentAsync(Arg.Is<StudentCourse>(sc => sc.Source == EnrollSource.Subscription));
    }

    [Fact] // UT-ENROL-07
    public async Task GetMyEnrolments_maps_from_repo()
    {
        _repo.GetEnrolmentsAsync(StudentId).Returns(new List<StudentCourse>
        {
            new() { StudentCourseId = 1, CourseId = 10, Course = new Course { Title = "Toán 6", Slug = "t6" } },
            new() { StudentCourseId = 2, CourseId = 11 },
        });

        var res = await Svc().GetMyEnrolmentsAsync(StudentId);

        res.Data.Should().HaveCount(2);
        res.Data[0].CourseTitle.Should().Be("Toán 6");
    }
}
