using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class RegisterViewModel
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; } // Student / Teacher

        // ✅ NEW
        [Required(ErrorMessage = "Full Name is required")]
        [MaxLength(150)]
        public string Name { get; set; }
    }
}
