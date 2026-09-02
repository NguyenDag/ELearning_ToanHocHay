using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Attributes
{
    /// <summary>
    /// Allows the content-management roles: ContentEditor, AcademicReviewer, SystemAdmin.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class AuthorizeContentRoleAttribute : AuthorizeUserTypeAttribute
    {
        public AuthorizeContentRoleAttribute()
            : base(UserType.ContentEditor, UserType.AcademicReviewer, UserType.SystemAdmin)
        {
        }
    }
}
