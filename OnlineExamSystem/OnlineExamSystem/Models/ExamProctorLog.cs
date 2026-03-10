using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineExamSystem.Models
{
    public class ExamProctorLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExamAttemptId { get; set; }

        [ForeignKey("ExamAttemptId")]
        public virtual ExamAttempt ExamAttempt { get; set; }

        [Required]
        public string ImagePath { get; set; }

        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

        // Future phase 2
        public bool? SuspiciousFlag { get; set; }
    }
}
