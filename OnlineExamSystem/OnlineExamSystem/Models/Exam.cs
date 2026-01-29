using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class Exam
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public int DurationInMinutes { get; set; }

        // NOTE:
        // This will later be derived from Question.Marks,
        // but for now we keep it to avoid breaking existing logic.
        [Required]
        public int TotalMarks { get; set; }

        // -------------------------------
        // OWNERSHIP (LOCKED)
        // -------------------------------
        // The teacher (UserId) who created this exam
        [Required]
        public int CreatedByTeacherId { get; set; }

        // -------------------------------
        // PHASE 2: ASSIGNMENT & TIMING
        // -------------------------------

        // Assigned College (Admin-created, Student-selected)
        public int? CollegeId { get; set; }

        // Assigned Course (Admin-created, Student-selected)
        public int? CourseId { get; set; }

        // Exam availability window
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }

        // -------------------------------
        // METADATA
        // -------------------------------

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Question> Questions { get; set; }
    }
}
