using System.Threading.Tasks;

namespace OnlineExamSystem.Services.AdminCleanup
{
    public interface IAdminCleanupService
    {
        Task DeleteCollegeCompletelyAsync(int collegeId);
    }
}