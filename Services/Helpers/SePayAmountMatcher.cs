namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// So số tiền IPN với số tiền kỳ vọng trong dung sai (VND). Tách khỏi
    /// <see cref="Implementations.SePayIpnService"/> để test biên dung sai.
    /// <paramref name="expected"/> được làm tròn về số nguyên; dung sai âm coi như 0.
    /// </summary>
    public static class SePayAmountMatcher
    {
        public static bool Matches(decimal expected, decimal actual, decimal toleranceVnd)
        {
            var exp = Math.Round(expected, MidpointRounding.AwayFromZero);
            var tol = Math.Max(0m, toleranceVnd);
            return Math.Abs(actual - exp) <= tol;
        }
    }
}
