using ELearning_ToanHocHay_Control.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Common
{
    public static class ControllerBaseExtensions
    {
        /// <summary>A5 — 403 with the standard <see cref="ApiResponse{T}"/> envelope.</summary>
        public static ObjectResult Forbidden(
            this ControllerBase controller,
            string message = "Bạn không có quyền thực hiện thao tác này")
            => controller.StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Forbidden(message));

        /// <summary>
        /// A5 — turns an <see cref="ApiResponse{T}"/> into an <see cref="IActionResult"/> whose
        /// HTTP status matches the response: <see cref="ApiResponse{T}.StatusCode"/> when it was set
        /// explicitly (201 / 403 / 404 / 409), otherwise inferred — a "not found" failure → 404,
        /// any other failure → 400, success → 200.
        /// </summary>
        public static ActionResult ToActionResult<T>(this ApiResponse<T> response)
        {
            var status = response.StatusCode;

            if (!response.Success && status == 400)
            {
                var msg = response.Message?.ToLowerInvariant() ?? "";
                if (msg.Contains("not found") || msg.Contains("không tìm thấy")
                    || msg.Contains("không tồn tại") || msg.Contains("does not exist"))
                    status = StatusCodes.Status404NotFound;
            }

            return new ObjectResult(response) { StatusCode = status };
        }
    }
}
