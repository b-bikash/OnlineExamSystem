using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineExamSystem.Models
{
    public class ExamAttempt
    {
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }
        public Student Student { get; set; }

        [Required]
        public int ExamId { get; set; }
        public Exam Exam { get; set; }

        // Enforce college-level data isolation
        [Required]
        public int CollegeId { get; set; }
        public College College { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public int Score { get; set; }

        public ICollection<StudentAnswer> StudentAnswers { get; set; }
        
        // Phase 1/2: Proctoring Logs
        public ICollection<ExamProctorLog> ExamProctorLogs { get; set; }

        public int FullScreenExitCount { get; set; } = 0;
    }
}