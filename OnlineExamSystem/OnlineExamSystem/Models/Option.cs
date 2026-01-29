using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class Option
    {
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }
        public Question Question { get; set; }

        [Required]
        [MaxLength(500)]
        public string Text { get; set; }

        public bool IsCorrect { get; set; }
    }
}
