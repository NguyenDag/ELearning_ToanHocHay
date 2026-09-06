namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// P1 — công thức khoá đăng nhập leo thang, tách khỏi <see cref="Implementations.AuthService"/>
    /// để test không cần user / DB. Bắt đầu khoá từ lần sai thứ 5: 1, 2, 4, 8, 16 … phút, cap 30.
    /// </summary>
    public static class LoginThrottlePolicy
    {
        public const int MaxFailedAttempts = 5;
        public static readonly TimeSpan BaseLockout = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan MaxLockout = TimeSpan.FromMinutes(30);

        /// <returns>
        /// Thời lượng khoá tương ứng số lần đăng nhập sai tích luỹ, hoặc <c>null</c> nếu chưa tới ngưỡng khoá.
        /// </returns>
        public static TimeSpan? NextLockout(int failedCount)
        {
            if (failedCount < MaxFailedAttempts) return null;

            var over = failedCount - MaxFailedAttempts;
            var minutes = Math.Min(MaxLockout.TotalMinutes, BaseLockout.TotalMinutes * Math.Pow(2, over));
            return TimeSpan.FromMinutes(minutes);
        }
    }
}
