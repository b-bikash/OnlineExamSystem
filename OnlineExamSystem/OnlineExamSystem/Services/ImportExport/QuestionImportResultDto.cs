using System.Collections.Generic;

namespace OnlineExamSystem.Services.ImportExport
{
    public class QuestionImportResultDto
    {
        public int TotalRows { get; set; }
        public int InsertedQuestions { get; set; }
        public int InsertedOptions { get; set; }
        public int InvalidRows { get; set; }

        public List<string> Errors { get; set; } = new();
    }
}