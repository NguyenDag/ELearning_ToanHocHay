using System.Text.Json.Serialization;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; }

        /// <summary>
        /// A5 — the HTTP status this response maps to (200 success / 400 bad request /
        /// 404 not found / 403 forbidden / 409 conflict). Not serialised.
        /// </summary>
        [JsonIgnore]
        public int StatusCode { get; set; } = 200;

        public ApiResponse()
        {
            Errors = new List<string>();
        }

        public static ApiResponse<T> SuccessResponse(T data, string message = "Thành công")
            => new() { Success = true, Message = message, Data = data, StatusCode = 200 };

        public static ApiResponse<T> Created(T data, string message = "Đã tạo")
            => new() { Success = true, Message = message, Data = data, StatusCode = 201 };

        public static ApiResponse<T> ErrorResponse(string message, List<string> errors = null)
            => new() { Success = false, Message = message, Errors = errors ?? new(), StatusCode = 400 };

        public static ApiResponse<T> NotFound(string message = "Không tìm thấy")
            => new() { Success = false, Message = message, StatusCode = 404 };

        public static ApiResponse<T> Forbidden(string message = "Bạn không có quyền thực hiện thao tác này")
            => new() { Success = false, Message = message, StatusCode = 403 };

        public static ApiResponse<T> Conflict(string message)
            => new() { Success = false, Message = message, StatusCode = 409 };
    }
}
