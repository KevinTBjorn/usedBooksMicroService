using NotificationService.Models;

namespace NotificationService.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailRequest request);
    }
}
