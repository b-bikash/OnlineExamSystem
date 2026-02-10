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
        // Can later be derived from Questions
        [Required]
        public int TotalMarks { get; set; }

        // -------------------------------
        // OWNERSHIP (SOURCE OF TRUTH)
        // -------------------------------

        // Teacher who created the exam
        [Required]
        public int CreatedByTeacherId { get; set; }
        public Teacher CreatedByTeacher { get; set; }

        // -------------------------------
        // ACADEMIC CONTEXT (LOCKED)
        // -------------------------------

        // Exam is always for ONE subject
        [Required]
        public int SubjectId { get; set; }
        public Subject Subject { get; set; }

        // -------------------------------
        // TIMING
        // -------------------------------

        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }

        // -------------------------------
        // METADATA
        // -------------------------------
        public int CollegeId { get; set; }
public College College { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Question> Questions { get; set; }
        public ICollection<ExamAttempt> ExamAttempts { get; set; }
    }
}