using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Services.Helpers;
using FluentAssertions;

namespace ELearning_ToanHocHay.Tests.Unit.Grading;

/// <summary>§3.13 — UT-GRADE-*. Chấm điểm thuần (U1), không DB.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U1")]
public class AnswerGradingTests
{
    private static Question Q(QuestionType type, string? correct = null, params QuestionOption[] options)
        => new()
        {
            QuestionText = "Câu hỏi?",
            QuestionType = type,
            CorrectAnswer = correct,
            QuestionOptions = options.ToList(),
        };

    private static QuestionOption Opt(int id, bool correct)
        => new() { OptionId = id, OptionText = $"opt{id}", IsCorrect = correct };

    private static StudentAnswer A(int? optionId = null, string? text = null)
        => new() { SelectedOptionId = optionId, AnswerText = text };

    // ---- MultipleChoice ----

    [Theory]
    [InlineData(1, true, false)]   // UT-GRADE-01
    [InlineData(2, false, false)]  // UT-GRADE-02
    public void GradeAnswer_MultipleChoice_scores_by_selected_option(int selected, bool isCorrect, bool needsManual)
    {
        var q = Q(QuestionType.MultipleChoice, options: new[] { Opt(1, true), Opt(2, false) });

        AnswerGrading.GradeAnswer(q, A(optionId: selected)).Should().Be((isCorrect, needsManual));
    }

    [Fact] // UT-GRADE-03
    public void GradeAnswer_MultipleChoice_no_selection_is_wrong()
        => AnswerGrading.GradeAnswer(Q(QuestionType.MultipleChoice, options: new[] { Opt(1, true) }), A())
            .Should().Be((false, false));

    [Fact] // UT-GRADE-04
    public void GradeAnswer_MultipleChoice_no_correct_option_is_wrong()
        => AnswerGrading.GradeAnswer(Q(QuestionType.MultipleChoice, options: new[] { Opt(1, false), Opt(2, false) }), A(optionId: 1))
            .Should().Be((false, false));

    // ---- TrueFalse ----

    [Fact] // UT-GRADE-10
    public void GradeAnswer_TrueFalse_with_options_correct_selection()
        => AnswerGrading.GradeAnswer(Q(QuestionType.TrueFalse, options: new[] { Opt(10, true), Opt(11, false) }), A(optionId: 10))
            .Should().Be((true, false));

    [Fact] // UT-GRADE-11
    public void GradeAnswer_TrueFalse_with_options_wrong_selection()
        => AnswerGrading.GradeAnswer(Q(QuestionType.TrueFalse, options: new[] { Opt(10, true), Opt(11, false) }), A(optionId: 11))
            .Should().Be((false, false));

    [Fact] // UT-GRADE-12 — có option nhưng không chọn option; CorrectAnswer null → không chấm đúng
    public void GradeAnswer_TrueFalse_with_options_but_text_answer_is_wrong()
        => AnswerGrading.GradeAnswer(Q(QuestionType.TrueFalse, correct: null, options: new[] { Opt(10, true), Opt(11, false) }), A(text: "true"))
            .Should().Be((false, false));

    [Theory] // UT-GRADE-13..16
    [InlineData("true")]
    [InlineData("đúng")]
    [InlineData("1")]
    [InlineData("yes")]
    [InlineData("y")]
    [InlineData("t")]
    [InlineData("d")]
    public void GradeAnswer_TrueFalse_no_options_true_tokens(string text)
        => AnswerGrading.GradeAnswer(Q(QuestionType.TrueFalse, correct: "true"), A(text: text))
            .Should().Be((true, false));

    [Theory] // UT-GRADE-17
    [InlineData("false")]
    [InlineData("sai")]
    [InlineData("0")]
    [InlineData("n")]
    [InlineData("s")]
    public void GradeAnswer_TrueFalse_no_options_false_tokens_against_true_answer(string text)
        => AnswerGrading.GradeAnswer(Q(QuestionType.TrueFalse, correct: "true"), A(text: text))
            .Should().Be((false, false));

    [Fact] // UT-GRADE-18
    public void GradeAnswer_TrueFalse_unparseable_text_is_wrong()
        => AnswerGrading.GradeAnswer(Q(QuestionType.TrueFalse, correct: "true"), A(text: "maybe"))
            .Should().Be((false, false));

    // ---- FillBlank / Essay / null ----

    [Fact] // UT-GRADE-20
    public void GradeAnswer_Essay_needs_manual_grading()
        => AnswerGrading.GradeAnswer(Q(QuestionType.Essay), A(text: "một đoạn văn"))
            .Should().Be((false, true));

    [Fact] // UT-GRADE-21
    public void GradeAnswer_null_question()
        => AnswerGrading.GradeAnswer(null, A(text: "x")).Should().Be((false, false));

    [Fact] // UT-GRADE-22
    public void GradeAnswer_null_answer()
        => AnswerGrading.GradeAnswer(Q(QuestionType.FillBlank, "1"), null).Should().Be((false, false));

    [Fact] // UT-GRADE-23
    public void GradeAnswer_unknown_question_type()
        => AnswerGrading.GradeAnswer(Q((QuestionType)99), A(text: "x")).Should().Be((false, false));

    // ---- IsFillBlankCorrect ----

    [Theory]
    [InlineData("1/2", "0.5", true)]      // UT-GRADE-30
    [InlineData("0.5", "1/2", true)]      // UT-GRADE-31
    [InlineData("1/2", "0,5", true)]      // UT-GRADE-32
    [InlineData("1/2", " 0.5 ", true)]    // UT-GRADE-33
    [InlineData("2", "2.0", true)]        // UT-GRADE-34
    [InlineData("0.5", "0.50", true)]     // UT-GRADE-35
    [InlineData("abc", "abc", true)]      // UT-GRADE-36
    [InlineData("abc", "ABC", true)]      // UT-GRADE-37
    [InlineData("1/2", "0.6", false)]     // UT-GRADE-38
    [InlineData("1/2", "", false)]        // UT-GRADE-39
    [InlineData("1/2", null, false)]      // UT-GRADE-39
    [InlineData(null, "0.5", false)]      // UT-GRADE-40
    [InlineData("", "0.5", false)]        // UT-GRADE-40
    [InlineData("2|two", "two", true)]    // UT-GRADE-41
    [InlineData("2;hai", "hai", true)]    // UT-GRADE-42
    [InlineData("1.234,56", "1234.56", true)]  // UT-GRADE-43
    [InlineData("1,234.56", "1234.56", true)]  // UT-GRADE-44
    [InlineData("1/0", "5", false)]       // UT-GRADE-45
    [InlineData("3/6", "1/2", true)]      // UT-GRADE-46
    public void IsFillBlankCorrect_cases(string? correct, string? student, bool expected)
        => AnswerGrading.IsFillBlankCorrect(student, correct).Should().Be(expected);

    // ---- Normalize / TryParseNumeric ----

    [Theory]
    [InlineData("  Hello   World  ", "hello world")]  // UT-GRADE-50
    [InlineData("1.234,56", "1234.56")]               // UT-GRADE-51
    [InlineData(null, "")]                            // UT-GRADE-52
    [InlineData("", "")]                              // UT-GRADE-52
    public void Normalize_cases(string? input, string expected)
        => AnswerGrading.Normalize(input).Should().Be(expected);

    [Fact] // UT-GRADE-53
    public void TryParseNumeric_fraction() => AnswerGrading.TryParseNumeric("3/4").Should().Be(0.75m);

    [Fact] // UT-GRADE-54
    public void TryParseNumeric_non_numeric() => AnswerGrading.TryParseNumeric("abc").Should().BeNull();

    [Fact] // UT-GRADE-55
    public void TryParseNumeric_divide_by_zero() => AnswerGrading.TryParseNumeric("5/0").Should().BeNull();
}
