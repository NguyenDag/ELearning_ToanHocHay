using AutoMapper;
using ELearning_ToanHocHay_Control.Data;
using ELearning_ToanHocHay_Control.Data.Entities;
using ELearning_ToanHocHay_Control.Models.DTOs.ExerciseAttempt;
using ELearning_ToanHocHay_Control.Repositories.Implementations;
using ELearning_ToanHocHay_Control.Services.Implementations;
using ELearning_ToanHocHay_Control.Services.Interfaces;
using ELearning_ToanHocHay.Tests.Unit.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace ELearning_ToanHocHay.Tests.Unit.Attempt;

/// <summary>§3.35 — UT-ATT-*. Sociable U2: repo thật trên SQLite, AI/notify/email là fake.</summary>
[Trait("Level", "Unit")]
[Trait("Tier", "U2")]
public class ExerciseAttemptServiceTests : IDisposable
{
    private readonly SqliteDb _sql = SqliteDb.New();
    private readonly IAiFeedbackQueue _queue = Substitute.For<IAiFeedbackQueue>();
    private readonly IProgressProjectionService _projection = Substitute.For<IProgressProjectionService>();
    private readonly INotificationRuleEngine _rules = Substitute.For<INotificationRuleEngine>();

    private readonly int _studentId;

    public ExerciseAttemptServiceTests()
        => _studentId = Seed.Student(_sql.Db).Student.StudentId;

    public void Dispose() => _sql.Dispose();

    private ExerciseAttemptService Build()
    {
        var ctx = _sql.NewContext();
        return new ExerciseAttemptService(
            new ExerciseAttemptRepository(ctx),
            new ExerciseRepository(ctx),
            new StudentAnswerRepository(ctx),
            new UserRepository(ctx),
            new QuestionBankRepository(ctx),
            new ExerciseQuestionRepository(ctx),
            new AIFeedbackRepository(ctx),
            _queue,
            Substitute.For<IMapper>(),
            ctx,
            Substitute.For<IEmailService>(),
            _projection,
            _rules,
            Substitute.For<IBackgroundEmailService>());
    }

    // ---------------------------------------------------------------- StartExercise

    [Fact] // UT-ATT-01
    public async Task Start_unpublished_exercise_is_unavailable()
    {
        var ex = Seed.Exercise(_sql.Db, tweak: e => e.Status = ExerciseStatus.Draft);

        var res = await Build().StartExerciseAsync(new StartExerciseDto { StudentId = _studentId, ExerciseId = ex.ExerciseId });

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("không khả dụng");
    }

    [Fact] // UT-ATT-02
    public async Task Start_premium_exercise_on_free_tier_is_forbidden()
    {
        var ex = Seed.Exercise(_sql.Db, tweak: e => { e.IsFree = false; e.RequiredTier = AccessTier.Premium; });

        var res = await Build().StartExerciseAsync(new StartExerciseDto { StudentId = _studentId, ExerciseId = ex.ExerciseId });

        res.Success.Should().BeFalse();
        res.StatusCode.Should().Be(403);
    }

    [Fact] // UT-ATT-03
    public async Task Start_after_max_attempts_is_rejected()
    {
        var ex = Seed.Exercise(_sql.Db, tweak: e => e.MaxAttempts = 1);
        Seed.Attempt(_sql.Db, _studentId, ex.ExerciseId, AttemptStatus.Submitted);

        var res = await Build().StartExerciseAsync(new StartExerciseDto { StudentId = _studentId, ExerciseId = ex.ExerciseId });

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("hết số lượt");
    }

    [Fact] // UT-ATT-04
    public async Task Start_valid_creates_in_progress_attempt_with_planned_end()
    {
        var ex = Seed.Exercise(_sql.Db, tweak: e => e.DurationMinutes = 30);

        var res = await Build().StartExerciseAsync(new StartExerciseDto { StudentId = _studentId, ExerciseId = ex.ExerciseId });

        res.Success.Should().BeTrue();
        var attempt = _sql.NewContext().ExerciseAttempts.Single();
        attempt.Status.Should().Be(AttemptStatus.InProgress);
        attempt.PlannedEndTime.Should().NotBeNull();
    }

    // ---------------------------------------------------------------- SaveAnswer

    [Fact] // UT-ATT-05
    public async Task SaveAnswer_twice_overwrites_the_same_row()
    {
        var bank = Seed.QuestionBank(_sql.Db);
        var q = Seed.Question(_sql.Db, bank.BankId, QuestionType.FillBlank, correct: "5");
        var ex = Seed.Exercise(_sql.Db);
        var attempt = Seed.Attempt(_sql.Db, _studentId, ex.ExerciseId);

        await Build().SaveAnswerAsync(new SaveAnswerDto { AttemptId = attempt.AttemptId, QuestionId = q.QuestionId, AnswerText = "3" });
        await Build().SaveAnswerAsync(new SaveAnswerDto { AttemptId = attempt.AttemptId, QuestionId = q.QuestionId, AnswerText = "5" });

        var answers = _sql.NewContext().StudentAnswers.Where(a => a.AttemptId == attempt.AttemptId).ToList();
        answers.Should().ContainSingle();
        answers[0].AnswerText.Should().Be("5");
    }

    [Fact] // UT-ATT-06
    public async Task SaveAnswer_on_a_submitted_attempt_is_rejected()
    {
        var ex = Seed.Exercise(_sql.Db);
        var attempt = Seed.Attempt(_sql.Db, _studentId, ex.ExerciseId, AttemptStatus.Submitted);

        var res = await Build().SaveAnswerAsync(new SaveAnswerDto { AttemptId = attempt.AttemptId, QuestionId = 1, AnswerText = "x" });

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("không còn hoạt động");
    }

    // ---------------------------------------------------------------- StartRandom

    [Fact] // UT-ATT-12
    public async Task StartRandom_takes_exactly_the_requested_count_without_a_phantom_timeout()
    {
        var bank = Seed.QuestionBank(_sql.Db);
        Seed.Question(_sql.Db, bank.BankId, QuestionType.MultipleChoice, options: new[] { ("A", true) });
        Seed.Question(_sql.Db, bank.BankId, QuestionType.FillBlank, correct: "1");

        var res = await Build().StartRandomExerciseAsync(new StartRandomExerciseDto
        {
            StudentId = _studentId,
            BankId = bank.BankId,
            ExerciseType = ExerciseType.Practice,
            NumberOfQuestions = 2,
            DurationMinutes = null,
        });

        res.Success.Should().BeTrue();
        res.Data.Questions.Should().HaveCount(2);
        res.Data.PlannedEndTime.Should().BeNull();
    }

    // ---------------------------------------------------------------- CompleteExercise

    private (int attemptId, int mcQ, int fbQ) SeedGradableAttempt(DateTime? plannedEnd = null)
    {
        var bank = Seed.QuestionBank(_sql.Db);
        var mc = Seed.Question(_sql.Db, bank.BankId, QuestionType.MultipleChoice, options: new[] { ("A", true), ("B", false) });
        var fb = Seed.Question(_sql.Db, bank.BankId, QuestionType.FillBlank, correct: "5");
        var ex = Seed.Exercise(_sql.Db);
        Seed.AttachQuestion(_sql.Db, ex.ExerciseId, mc.QuestionId, score: 5, order: 1);
        Seed.AttachQuestion(_sql.Db, ex.ExerciseId, fb.QuestionId, score: 5, order: 2);
        var attempt = Seed.Attempt(_sql.Db, _studentId, ex.ExerciseId, maxScore: 10, plannedEnd: plannedEnd);
        var correctOptionId = _sql.NewContext().QuestionOptions.Single(o => o.QuestionId == mc.QuestionId && o.IsCorrect).OptionId;
        Seed.Answer(_sql.Db, attempt.AttemptId, mc.QuestionId, optionId: correctOptionId);   // correct
        Seed.Answer(_sql.Db, attempt.AttemptId, fb.QuestionId, text: "9");                    // wrong
        return (attempt.AttemptId, mc.QuestionId, fb.QuestionId);
    }

    [Fact] // UT-ATT-07
    public async Task Complete_auto_grades_and_totals()
    {
        var (attemptId, _, _) = SeedGradableAttempt();

        var res = await Build().CompleteExerciseAsync(new CompleteExerciseDto { AttemptId = attemptId });

        res.Success.Should().BeTrue();
        res.Data.CorrectAnswers.Should().Be(1);
        res.Data.WrongAnswers.Should().Be(1);
        res.Data.TotalScore.Should().Be(5);
    }

    [Fact] // UT-ATT-08
    public async Task Complete_flags_essays_for_manual_grading_without_counting_them_wrong()
    {
        var bank = Seed.QuestionBank(_sql.Db);
        var essay = Seed.Question(_sql.Db, bank.BankId, QuestionType.Essay);
        var ex = Seed.Exercise(_sql.Db);
        Seed.AttachQuestion(_sql.Db, ex.ExerciseId, essay.QuestionId, score: 10);
        var attempt = Seed.Attempt(_sql.Db, _studentId, ex.ExerciseId, maxScore: 10);
        Seed.Answer(_sql.Db, attempt.AttemptId, essay.QuestionId, text: "Bài luận của em...");

        var res = await Build().CompleteExerciseAsync(new CompleteExerciseDto { AttemptId = attempt.AttemptId });

        res.Data.HasPendingManualGrading.Should().BeTrue();
        res.Data.WrongAnswers.Should().Be(0);
        _sql.NewContext().StudentAnswers.Single(a => a.QuestionId == essay.QuestionId)
            .NeedsManualGrading.Should().BeTrue();
    }

    [Fact] // UT-ATT-09
    public async Task Complete_enqueues_ai_feedback_for_wrong_answers()
    {
        var (attemptId, _, fbQ) = SeedGradableAttempt();

        await Build().CompleteExerciseAsync(new CompleteExerciseDto { AttemptId = attemptId });

        _queue.Received().Enqueue(attemptId, fbQ, Arg.Any<string>());
    }

    [Fact] // UT-ATT-10
    public async Task Complete_projects_the_attempt_once()
    {
        var (attemptId, _, _) = SeedGradableAttempt();

        await Build().CompleteExerciseAsync(new CompleteExerciseDto { AttemptId = attemptId });

        await _projection.Received(1).ProjectAttemptAsync(attemptId);
    }

    [Fact] // UT-ATT-11
    public async Task Complete_twice_does_not_regrade()
    {
        var (attemptId, _, _) = SeedGradableAttempt();
        await Build().CompleteExerciseAsync(new CompleteExerciseDto { AttemptId = attemptId });

        var second = await Build().CompleteExerciseAsync(new CompleteExerciseDto { AttemptId = attemptId });

        second.Success.Should().BeFalse();
        second.Message.Should().Contain("đã được nộp");
        await _projection.Received(1).ProjectAttemptAsync(attemptId);
    }

    [Fact] // UT-ATT-13
    public async Task Complete_after_planned_end_still_grades_as_timeout()
    {
        var (attemptId, _, _) = SeedGradableAttempt(plannedEnd: DateTime.UtcNow.AddMinutes(-5));

        var res = await Build().CompleteExerciseAsync(new CompleteExerciseDto { AttemptId = attemptId });

        res.Success.Should().BeTrue();
        _sql.NewContext().ExerciseAttempts.Single(a => a.AttemptId == attemptId)
            .Status.Should().Be(AttemptStatus.Timeout);
    }
}
