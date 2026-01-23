using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class Answer
    {
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }
        public Question Question { get; set; }

        [Required]
        public int ExamAttemptId { get; set; }
        public ExamAttempt ExamAttempt { get; set; }

        // A / B / C / D selected by student
        [Required]
        [MaxLength(1)]
        public string SelectedAnswer { get; set; }
    }
}
