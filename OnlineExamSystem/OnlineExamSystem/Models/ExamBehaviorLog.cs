using System;

namespace OnlineExamSystem.Models
{
    public class ExamBehaviorLog
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public int ExamId { get; set; }

        public int TabSwitchCount { get; set; } = 0;
        public int FullscreenExitCount { get; set; } = 0;

        public double? AvgTimePerQuestion { get; set; }
        public double? TotalExamTime { get; set; }

        public int ViolationCount { get; set; } = 0;

        public bool IsSuspicious { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}