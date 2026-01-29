namespace OnlineExamSystem.Models.ViewModels
{
    public class AdminExamListItemViewModel
    {
        public int ExamId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int DurationInMinutes { get; set; }
        public int TotalMarks { get; set; }

        public int? TeacherUserId { get; set; }
        public string TeacherName { get; set; }
    }
}
