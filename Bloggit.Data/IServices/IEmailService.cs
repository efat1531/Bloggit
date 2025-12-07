namespace Bloggit.Data.IServices;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string email, string confirmationLink);
}
