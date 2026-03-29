using System.Threading.Tasks;

namespace OnlineExamSystem.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}