using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

public class DummyEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // Có thể log ra Console hoặc để trống.
        return Task.CompletedTask;
    }
}