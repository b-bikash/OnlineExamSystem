using System;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class StudentAnswer
    {
        public int Id { get; set; }

        [Required]
        public int ExamAttemptId { get; set; }
        public ExamAttempt ExamAttempt { get; set; }

        [Required]
        public int QuestionId { get; set; }
        public Question Question { get; set; }

        [Required]
        public int SelectedOptionId { get; set; }
        public Option SelectedOption { get; set; }

        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    }
}
