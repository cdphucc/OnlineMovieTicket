using OnlineMovieTicket.Models;

namespace OnlineMovieTicket.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
        Task SendPasswordResetEmailAsync(string email, string resetUrl, string userName);
        Task SendInvoiceEmailAsync(string email, string userName, Booking booking);
    }
}