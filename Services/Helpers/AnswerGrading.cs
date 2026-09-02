using System.Globalization;
using System.Text.RegularExpressions;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// Auto-grading rules for a single answer. Handles every <see cref="QuestionType"/>,
    /// normalises fill-in-the-blank answers (decimals, fractions, whitespace) and flags
    /// essays for manual grading.
    /// </summary>
    public static class AnswerGrading
    {
        private static readonly string[] TrueTokens = { "true", "t", "1", "yes", "y", "dung", "đúng", "d" };
        private static readonly string[] FalseTokens = { "false", "f", "0", "no", "n", "sai", "s" };

        /// <returns>(isCorrect, needsManualGrading)</returns>
        public static (bool isCorrect, bool needsManual) GradeAnswer(Question? question, StudentAnswer? answer)
        {
            if (question == null || answer == null)
                return (false, false);

            switch (question.QuestionType)
            {
                case QuestionType.MultipleChoice:
                    return (IsMultipleChoiceCorrect(question, answer), false);

                case QuestionType.TrueFalse:
                    return (IsTrueFalseCorrect(question, answer), false);

                case QuestionType.FillBlank:
                    return (IsFillBlankCorrect(answer.AnswerText, question.CorrectAnswer), false);

                case QuestionType.Essay:
                    // Cannot be auto-graded — wait for a human.
                    return (false, true);

                default:
                    return (false, false);
            }
        }

        private static bool IsMultipleChoiceCorrect(Question question, StudentAnswer answer)
        {
            if (!answer.SelectedOptionId.HasValue) return false;
            var correct = question.QuestionOptions?.FirstOrDefault(o => o.IsCorrect);
            return correct != null && correct.OptionId == answer.SelectedOptionId.Value;
        }

        private static bool IsTrueFalseCorrect(Question question, StudentAnswer answer)
        {
            // Prefer an option selection when the question carries options.
            if (answer.SelectedOptionId.HasValue && question.QuestionOptions != null && question.QuestionOptions.Any())
            {
                var opt = question.QuestionOptions.FirstOrDefault(o => o.OptionId == answer.SelectedOptionId.Value);
                return opt != null && opt.IsCorrect;
            }

            var studentBool = ParseBool(answer.AnswerText);
            var correctBool = ParseBool(question.CorrectAnswer);
            return studentBool.HasValue && correctBool.HasValue && studentBool.Value == correctBool.Value;
        }

        public static bool IsFillBlankCorrect(string? studentText, string? correctAnswer)
        {
            if (string.IsNullOrWhiteSpace(studentText) || string.IsNullOrWhiteSpace(correctAnswer))
                return false;

            var student = Normalize(studentText);
            var studentNum = TryParseNumeric(studentText);

            // "|" separates several accepted answers.
            foreach (var candidate in correctAnswer.Split('|', ';'))
            {
                if (string.IsNullOrWhiteSpace(candidate)) continue;

                if (Normalize(candidate) == student)
                    return true;

                var candidateNum = TryParseNumeric(candidate);
                if (studentNum.HasValue && candidateNum.HasValue &&
                    Math.Abs(studentNum.Value - candidateNum.Value) < 1e-6m)
                    return true;
            }

            return false;
        }

        /// <summary>Trim, lower-case, collapse whitespace, drop thousands separators, unify the decimal separator.</summary>
        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var s = value.Trim().ToLowerInvariant();
            s = Regex.Replace(s, @"\s+", " ");
            // 1.234,56 (vi) / 1,234.56 (en) -> 1234.56
            s = Regex.Replace(s, @"(?<=\d)[.,](?=\d{3}\b)", "");
            s = s.Replace(',', '.');
            return s;
        }

        /// <summary>Parses a decimal or a simple fraction "a/b" into a decimal value.</summary>
        public static decimal? TryParseNumeric(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var s = Normalize(value).Replace(" ", "");

            var slash = s.IndexOf('/');
            if (slash > 0 && slash < s.Length - 1)
            {
                if (decimal.TryParse(s[..slash], NumberStyles.Any, CultureInfo.InvariantCulture, out var num) &&
                    decimal.TryParse(s[(slash + 1)..], NumberStyles.Any, CultureInfo.InvariantCulture, out var den) &&
                    den != 0)
                    return num / den;
                return null;
            }

            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        private static bool? ParseBool(string? value)
        {
            var s = Normalize(value).Replace(" ", "");
            if (TrueTokens.Contains(s)) return true;
            if (FalseTokens.Contains(s)) return false;
            return null;
        }
    }
}
