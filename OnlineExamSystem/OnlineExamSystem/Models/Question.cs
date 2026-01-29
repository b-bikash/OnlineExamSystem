using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required]
        public string Text { get; set; }

        // -------------------------------
        // PHASE 3: MARKS PER QUESTION
        // -------------------------------
        [Required]
        public int Marks { get; set; } = 0;

        public int ExamId { get; set; }
        public Exam Exam { get; set; }

        // Options for this question (2–4)
        public ICollection<Option> Options { get; set; }
    }
}
