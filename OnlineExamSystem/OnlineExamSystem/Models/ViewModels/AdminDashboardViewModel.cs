namespace OnlineExamSystem.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalColleges { get; set; }
        public int ActiveColleges { get; set; }

        public int TotalUsers { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }

        public int TotalExams { get; set; }
        public int LiveExams { get; set; }
        public int UpcomingExams { get; set; }
        public int PastExams { get; set; }
        public int TotalCourses { get; set; }
        public int TotalSubjects { get; set; }
    }
}