using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class Option
    {
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }
        public Question Question { get; set; }

        // Enforce college-level data isolation
        [Required]
        public int CollegeId { get; set; }
        public College College { get; set; }

        [Required]
        [MaxLength(500)]
        public string Text { get; set; }

        public bool IsCorrect { get; set; }
    }
}