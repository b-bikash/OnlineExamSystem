using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required]
        public string Text { get; set; }

        public int ExamId { get; set; }
        public Exam Exam { get; set; }
        //public ICollection<Answer> Answers { get; set; }
    }
}
