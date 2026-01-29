using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class QuestionCreateViewModel
    {
        // -----------------------------
        // Question-level data
        // -----------------------------

        [Required]
        [MaxLength(1000)]
        public string QuestionText { get; set; }

        [Required]
        public int ExamId { get; set; }

        // -----------------------------
        // PHASE 3: MARKS
        // -----------------------------

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Marks must be 0 or greater.")]
        public int Marks { get; set; } = 0;

        // -----------------------------
        // Option-level data
        // -----------------------------

        // Holds option texts (2–4 required)
        public List<string> Options { get; set; } = new List<string>();

        // Index of the correct option (radio-button style)
        public int CorrectOptionIndex { get; set; }
    }
}
