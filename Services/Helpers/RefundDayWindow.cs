namespace ELearning_ToanHocHay_Control.Services.Helpers
{
    /// <summary>
    /// Mốc 00:00 "giờ địa phương" (Asia/Ho_Chi_Minh mặc định) quy về UTC — dùng để tính trần
    /// hoàn tiền theo ngày. Tách khỏi <see cref="Implementations.RefundService"/> để test mốc ngày.
    /// </summary>
    public static class RefundDayWindow
    {
        public static DateTime StartOfDayUtc(DateTime nowUtc, int offsetHours)
        {
            var offset = TimeSpan.FromHours(offsetHours);
            var localMidnight = (nowUtc + offset).Date;
            return DateTime.SpecifyKind(localMidnight - offset, DateTimeKind.Utc);
        }
    }
}
