using ELearning_ToanHocHay_Control.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ELearning_ToanHocHay_Control.Common
{
    public static class ControllerBaseExtensions
    {
        /// <summary>
        /// Returns 403 with a consistent <see cref="ApiResponse{T}"/> envelope.
        /// </summary>
        public static ObjectResult Forbidden(
            this ControllerBase controller,
            string message = "You do not have permission to access this resource")
        {
            return controller.StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<object>.ErrorResponse(message));
        }
    }
}
