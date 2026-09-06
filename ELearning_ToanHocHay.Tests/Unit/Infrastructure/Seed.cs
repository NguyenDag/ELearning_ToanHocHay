using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay.Tests.Unit.Infrastructure;

/// <summary>
/// Chèn nhanh đồ thị entity hợp lệ (thoả khoá ngoại SQLite) cho tầng U2. Mỗi hàm lưu ngay.
/// </summary>
public static class Seed
{
    public static (User User, Student Student) Student(AppDbContext db, int? gradeId = 1, Action<Student>? tweak = null)
    {
        var user = Entities.NewUser(type: UserType.Student);
        db.Users.Add(user);
        db.SaveChanges();

        var student = Entities.NewStudent(user.UserId, gradeId, tweak);
        db.Students.Add(student);
        db.SaveChanges();
        return (user, student);
    }

    public static User User(AppDbContext db, UserType type = UserType.Student, Action<User>? tweak = null)
    {
        var user = Entities.NewUser(type: type, tweak: tweak);
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Package Package(AppDbContext db, PackageTier tier = PackageTier.Standard, Action<Package>? tweak = null)
    {
        var owner = User(db, UserType.ContentEditor);
        var package = Entities.NewPackage(owner.UserId, tier, tweak);
        db.Packages.Add(package);
        db.SaveChanges();
        return package;
    }

    public static Payment Payment(AppDbContext db, decimal amount, PaymentStatus status, Action<Payment>? tweak = null)
    {
        var payer = User(db, UserType.Parent);
        var payment = Entities.NewPayment(payer.UserId, amount, status, tweak);
        db.Payments.Add(payment);
        db.SaveChanges();
        return payment;
    }

    public static CourseVersion CourseVersion(AppDbContext db, VersionState state = VersionState.Published)
    {
        var course = Entities.NewCourse(CourseStatus.Published);
        db.Courses.Add(course);
        db.SaveChanges();

        var cv = new CourseVersion { CourseId = course.CourseId, VersionNumber = 1, State = state, Label = "v1" };
        db.CourseVersions.Add(cv);
        db.SaveChanges();
        return cv;
    }

    /// <summary>Node có <c>MaterializedPath = "/{parentPath}{ownId}/"</c> (đúng quy ước của service).</summary>
    public static ContentNode Node(
        AppDbContext db, int courseVersionId, NodeType type,
        ContentNode? parent = null, bool isFree = true, bool isHidden = false)
    {
        var node = new ContentNode
        {
            CourseVersionId = courseVersionId,
            NodeType = type,
            Title = type.ToString(),
            ParentNodeId = parent?.NodeId,
            Depth = parent is null ? 0 : parent.Depth + 1,
            IsFree = isFree,
            IsHidden = isHidden,
            MaterializedPath = "/",
            CreatedBy = 1,
        };
        db.ContentNodes.Add(node);
        db.SaveChanges();

        node.MaterializedPath = (parent?.MaterializedPath ?? "/") + node.NodeId + "/";
        db.SaveChanges();
        return node;
    }

    public static Exercise Exercise(AppDbContext db, int? nodeId, Action<Exercise>? tweak = null)
    {
        var creator = User(db, UserType.ContentEditor);
        var ex = new Exercise
        {
            ExerciseName = "Bài tập",
            NodeId = nodeId,
            CreatedBy = creator.UserId,
            IsActive = true,
        };
        tweak?.Invoke(ex);
        db.Exercises.Add(ex);
        db.SaveChanges();
        return ex;
    }
}
