using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineExamSystem.Models
{
    [Table("Answer")]
    public class Answer
    {
        public int Id { get; set; }

        public int ExamAttemptId { get; set; }
        public ExamAttempt ExamAttempt { get; set; }

        public int QuestionId { get; set; }
        public Question Question { get; set; }

        public string SelectedAnswer { get; set; }
    }
}
