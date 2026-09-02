using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Helpers;
using FluentAssertions;

namespace ELearning_ToanHocHay_Control.Tests;

/// <summary>A2-09 — pure grading rules (no database needed).</summary>
public class AnswerGradingTests
{
    [Theory]
    [InlineData("1/2", "0.5", true)]
    [InlineData("0.5", "1/2", true)]
    [InlineData("1/2", "0,5", true)]     // decimal comma
    [InlineData("1/2", " 0.5 ", true)]   // whitespace
    [InlineData("2", "2.0", true)]
    [InlineData("0.5", "0.50", true)]
    [InlineData("abc", "abc", true)]
    [InlineData("abc", "ABC", true)]     // case-insensitive
    [InlineData("1/2", "0.6", false)]
    [InlineData("1/2", "", false)]
    [InlineData("2|two", "two", true)]   // multiple accepted answers
    public void FillBlank_normalises_numbers_fractions_and_whitespace(string correct, string student, bool expected)
    {
        AnswerGrading.IsFillBlankCorrect(student, correct).Should().Be(expected);
    }

    [Fact]
    public void Essay_always_needs_manual_grading()
    {
        var q = new Question { QuestionText = "x", QuestionType = QuestionType.Essay };
        var a = new StudentAnswer { AnswerText = "some long answer" };

        var (isCorrect, needsManual) = AnswerGrading.GradeAnswer(q, a);

        isCorrect.Should().BeFalse();
        needsManual.Should().BeTrue();
    }

    [Fact]
    public void TrueFalse_matches_by_selected_option()
    {
        var q = new Question
        {
            QuestionText = "x",
            QuestionType = QuestionType.TrueFalse,
            QuestionOptions = new List<QuestionOption>
            {
                new() { OptionId = 10, OptionText = "True", IsCorrect = true },
                new() { OptionId = 11, OptionText = "False", IsCorrect = false }
            }
        };

        AnswerGrading.GradeAnswer(q, new StudentAnswer { SelectedOptionId = 10 }).isCorrect.Should().BeTrue();
        AnswerGrading.GradeAnswer(q, new StudentAnswer { SelectedOptionId = 11 }).isCorrect.Should().BeFalse();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("đúng", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("sai", false)]
    public void TrueFalse_matches_by_text_when_no_option(string studentText, bool expectCorrect)
    {
        var q = new Question { QuestionText = "x", QuestionType = QuestionType.TrueFalse, CorrectAnswer = "true" };
        AnswerGrading.GradeAnswer(q, new StudentAnswer { AnswerText = studentText }).isCorrect.Should().Be(expectCorrect);
    }

    [Fact]
    public void Missing_answer_is_not_correct()
    {
        var q = new Question { QuestionText = "x", QuestionType = QuestionType.MultipleChoice };
        AnswerGrading.GradeAnswer(q, null).Should().Be((false, false));
    }
}
