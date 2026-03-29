using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace OnlineExamSystem.Services.ImportExport
{
    public interface IQuestionImportService
    {
        Task<QuestionImportResultDto> ImportQuestionsAsync(
            IFormFile file,
            int examId,
            int collegeId);
    }
}