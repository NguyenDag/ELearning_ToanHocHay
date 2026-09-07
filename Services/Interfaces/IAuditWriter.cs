namespace ELearning_ToanHocHay_Control.Services.Interfaces
{
    /// <summary>
    /// Ghi một dòng <c>AuditLog</c> cho các thao tác quản trị (tạo/sửa/xoá người dùng, đổi cấu hình,
    /// CRUD nội dung...). Người thực hiện + IP tự lấy từ <c>IHttpContextAccessor</c> nếu không truyền.
    /// Bổ trợ cho <c>AuditSaveChangesInterceptor</c> (vốn chỉ bắt thay đổi field nhạy cảm).
    /// Không bao giờ ném — lỗi ghi audit chỉ log warning.
    /// </summary>
    public interface IAuditWriter
    {
        Task WriteAsync(
            string action,
            string entityType,
            int? entityId = null,
            object? oldValue = null,
            object? newValue = null,
            int? actorUserId = null,
            string? ip = null,
            CancellationToken ct = default);
    }
}
