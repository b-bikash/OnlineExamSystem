using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class QuestionEditViewModel
    {
        // -----------------------------
        // Question identity
        // -----------------------------
        [Required]
        public int QuestionId { get; set; }

        [Required]
        public int ExamId { get; set; }

        // -----------------------------
        // Editable question data
        // -----------------------------
        [Required]
        [MaxLength(1000)]
        public string QuestionText { get; set; }

        [Range(0, int.MaxValue)]
        public int Marks { get; set; }

        // -----------------------------
        // Options (2–4, optional rows allowed)
        // -----------------------------
        public List<OptionEditItemViewModel> Options { get; set; }
            = new List<OptionEditItemViewModel>();

        // -----------------------------
        // Correct answer
        // -----------------------------
        [Required]
        public int CorrectOptionId { get; set; }
    }

    // -----------------------------
    // Nested Option ViewModel
    // -----------------------------
    public class OptionEditItemViewModel
    {
        // Existing option → Id > 0
        // New option → Id = 0
        public int OptionId { get; set; }

        // ❌ NO [Required] HERE (OPTIONAL ROWS)
        [MaxLength(500)]
        public string Text { get; set; }
    }
}
