using System.Collections.Generic;

namespace OnlineExamSystem.Services.ImportExport
{
    public class ImportResultDto
    {
        public int TotalRows { get; set; }
        public int InsertedCourses { get; set; }
        public int InsertedSubjects { get; set; }
        public int InsertedMappings { get; set; }
        public int SkippedDuplicates { get; set; }
        public int InvalidRows { get; set; }

        public List<string> Errors { get; set; } = new();
    }
}