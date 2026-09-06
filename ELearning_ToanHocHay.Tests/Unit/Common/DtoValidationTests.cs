using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs;
using ELearning_ToanHocHay_Control.Models.DTOs.Refund;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Common;

/// <summary>§3.38 — UT-DTO-*. `Validator.TryValidateObject` với `validateAllProperties: true`.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class DtoValidationTests
{
    private static bool IsValid(object dto)
        => Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), validateAllProperties: true);

    [Theory] // UT-DTO-01 / 02 / 03
    [InlineData("", "pw123456", false)]
    [InlineData("not-an-email", "pw123456", false)]
    [InlineData("ok@test.local", "", false)]
    [InlineData("ok@test.local", "pw123456", true)]
    public void LoginRequestDto(string email, string password, bool expected)
        => IsValid(new LoginRequestDto { Email = email, Password = password }).Should().Be(expected);

    [Fact] // UT-DTO-04
    public void RegisterRequestDto_short_password_is_invalid()
        => IsValid(new RegisterRequestDto
        {
            Email = "ok@test.local", Password = "123", ConfirmPassword = "123",
            FullName = "Tên", UserType = UserType.Student,
        }).Should().BeFalse();

    [Fact] // UT-DTO-05
    public void RegisterRequestDto_complete_is_valid()
        => IsValid(new RegisterRequestDto
        {
            Email = "ok@test.local", Password = "pw123456", ConfirmPassword = "pw123456",
            FullName = "Tên Học Sinh", UserType = UserType.Student,
        }).Should().BeTrue();

    [Theory] // UT-DTO-06
    [InlineData(null, "new-pw-123")]
    [InlineData("old-pw", null)]
    public void ChangePasswordDto_missing_field_is_invalid(string? current, string? next)
        => IsValid(new ChangePasswordDto { CurrentPassword = current!, NewPassword = next! }).Should().BeFalse();

    [Fact] // UT-DTO-07
    public void CreateRefundRequestDto_empty_is_invalid()
        => IsValid(new CreateRefundRequestDto()).Should().BeFalse();

    [Fact] // UT-DTO-08
    public void CreateRefundRequestDto_bad_bank_bin_is_invalid()
        => IsValid(new CreateRefundRequestDto
        {
            PaymentId = 1, ReasonCode = RefundReasonCode.CustomerRequest,
            BankBin = "abc", BankAccountNumber = "0071000123456", BankAccountHolderName = "A",
        }).Should().BeFalse();

    [Theory] // UT-DTO-10
    [InlineData("", "new-pw-123")]
    [InlineData("tok", "123")]
    public void ResetPasswordDto_missing_or_short_is_invalid(string token, string newPassword)
        => IsValid(new ResetPasswordDto { Token = token, NewPassword = newPassword }).Should().BeFalse();
}
