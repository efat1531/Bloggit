using Bloggit.Data.IServices;
using Microsoft.Extensions.Logging;

namespace Bloggit.Data.Services;

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    private readonly ILogger<EmailService> _logger = logger;

    public Task SendEmailConfirmationAsync(string email, string confirmationLink)
    {
        // TODO: Implement actual email sending (SMTP, SendGrid, etc.)
        // For now, log the confirmation link to console
        _logger.LogInformation("Email confirmation link for {Email}: {ConfirmationLink}", email, confirmationLink);

        // Simulate async email sending
        return Task.CompletedTask;
    }
}
