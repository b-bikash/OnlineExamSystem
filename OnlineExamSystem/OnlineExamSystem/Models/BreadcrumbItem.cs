namespace OnlineExamSystem.Models
{
    public class BreadcrumbItem
    {
        /// <summary>
        /// Text displayed in breadcrumb (e.g. "Exams", "Questions")
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Target URL. Null for the active (current) page.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// True if this breadcrumb represents the current page
        /// </summary>
        public bool IsActive { get; set; }
    }
}
