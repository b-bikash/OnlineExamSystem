using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}