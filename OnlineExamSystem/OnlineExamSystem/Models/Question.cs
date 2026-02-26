using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required]
        public string Text { get; set; }

        [Required]
        public int Marks { get; set; } = 1;

        [Required]
        public int ExamId { get; set; }
        public Exam Exam { get; set; }

        // Enforce college-level data isolation
        [Required]
        public int CollegeId { get; set; }
        public College College { get; set; }

        public ICollection<Option> Options { get; set; }

        public string? ImageUrl { get; set; }
    }
}