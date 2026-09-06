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
}
