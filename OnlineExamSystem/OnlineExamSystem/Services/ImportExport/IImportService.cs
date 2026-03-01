using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace OnlineExamSystem.Services.ImportExport
{
    public interface IImportService
    {
        Task<ImportResultDto> ImportCourseSubjectAsync(
            IFormFile file,
            int collegeId);
    }
}