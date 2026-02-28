namespace OnlineExamSystem.ViewModels
{
    public class TeacherAdminDashboardViewModel
    {
        // Core Counts
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalExams { get; set; }

        // Exam Status Breakdown
        public int LiveExamsCount { get; set; }
        public int UpcomingExamsCount { get; set; }
        public int CompletedExamsCount { get; set; }

        // Exam Activity
        public int TotalExamAttempts { get; set; }

        // Future Enhancement Placeholder
        public double? AverageScore { get; set; }
    }
}