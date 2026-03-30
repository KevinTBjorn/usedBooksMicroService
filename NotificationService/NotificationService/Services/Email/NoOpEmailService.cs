using NotificationService.Models;

namespace NotificationService.Services.Email
{
    public class NoOpEmailService : IEmailService
    {
        private readonly ILogger<NoOpEmailService> _logger;

        public NoOpEmailService(ILogger<NoOpEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(EmailRequest request)
        {
            _logger.LogInformation(
                "Simulated email: To={To}, Subject={Subject}, Body={Body}",
                request.To, request.Subject, request.Body);

            return Task.CompletedTask;
        }
    }
}
