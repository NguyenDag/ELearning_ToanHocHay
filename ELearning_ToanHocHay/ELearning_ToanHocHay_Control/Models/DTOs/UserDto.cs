using System.ComponentModel.DataAnnotations;
using ELearning_ToanHocHay_Control.Data.Entities;

namespace ELearning_ToanHocHay_Control.Models.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string? Phone { get; set; }
        public DateTime? Dob { get; set; }
        public string? AvatarUrl { get; set; }
        public UserType UserType { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateUserDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string FullName { get; set; }
        public string? Phone { get; set; }
        public DateTime? Dob { get; set; }
        public string? AvatarUrl { get; set; }
        public UserType UserType { get; set; }
    }

    public class UpdateUserDto
    {
        public required string Password { get; set; }
        public required string FullName { get; set; }
        public string? Phone { get; set; }
        public DateTime? Dob { get; set; }
        public string? AvatarUrl { get; set; }
        public UserType UserType { get; set; }
        public bool IsActive { get; set; }
    }
}
