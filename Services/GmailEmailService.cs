using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using OnlineMovieTicket.Models;

namespace OnlineMovieTicket.Services
{
    public class GmailEmailService : IEmailSender, IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GmailEmailService> _logger;

        public GmailEmailService(IConfiguration configuration, ILogger<GmailEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(
                        _configuration["Gmail:Username"],
                        _configuration["Gmail:AppPassword"]),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["Gmail:Username"], "Online Movie Ticket"),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(email);
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation($"Email sent successfully to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {email}");
                // Don't throw - just log the error
            }
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetUrl, string userName)
        {
            var subject = "Đặt lại mật khẩu - Online Movie Ticket";
            var htmlContent = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <div style='background: #e50914; color: white; padding: 20px; text-align: center;'>
        <h1>🎬 Online Movie Ticket</h1>
    </div>
    <div style='padding: 30px; background: #f9f9f9;'>
        <h2>Xin chào {userName}!</h2>
        <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản của mình.</p>
        <p>Vui lòng nhấp vào nút bên dưới để đặt lại mật khẩu:</p>
        <p style='text-align: center;'>
            <a href='{resetUrl}' style='display: inline-block; padding: 12px 24px; background: #e50914; color: white; text-decoration: none; border-radius: 5px;'>
                Đặt lại mật khẩu
            </a>
        </p>
        <p><strong>Lưu ý:</strong> Link này sẽ hết hạn sau 1 giờ.</p>
        <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
    </div>
    <div style='text-align: center; padding: 20px; color: #666; font-size: 12px;'>
        <p>© 2025 Online Movie Ticket. All rights reserved.</p>
    </div>
</div>";

            await SendEmailAsync(email, subject, htmlContent);
        }

        public async Task SendInvoiceEmailAsync(string email, string userName, Booking booking)
        {
            var subject = $"Hóa đơn thanh toán vé phim - Đơn hàng #{booking.Id}";
            var movieTitle = booking.BookingDetails?.FirstOrDefault()?.ShowTime?.Movie?.Title ?? "N/A";
            var showTime = booking.BookingDetails?.FirstOrDefault()?.ShowTime?.StartTime.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
            var seats = string.Join(", ", booking.BookingDetails?.Select(bd => bd.Seat?.SeatNumber) ?? new List<string>());

            var htmlContent = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <div style='background: #e50914; color: white; padding: 20px; text-align: center;'>
        <h1>🎬 HÓA ĐƠN VÉ PHIM</h1>
    </div>
    <div style='padding: 30px; background: #f9f9f9;'>
        <h2>Xin chào {userName}!</h2>
        <p>Cảm ơn bạn đã đặt vé tại Online Movie Ticket!</p>
        
        <div style='background: white; padding: 20px; margin: 20px 0; border-radius: 5px;'>
            <h3>Thông tin đơn hàng</h3>
            <p><strong>Mã đơn hàng:</strong> #{booking.Id}</p>
            <p><strong>Ngày đặt:</strong> {booking.BookingTime:dd/MM/yyyy HH:mm}</p>
            <p><strong>Phim:</strong> {movieTitle}</p>
            <p><strong>Suất chiếu:</strong> {showTime}</p>
            <p><strong>Ghế:</strong> {seats}</p>
            <p><strong>Tổng tiền:</strong> <span style='color: #e50914; font-size: 18px; font-weight: bold;'>{booking.TotalAmount:N0} VNĐ</span></p>
        </div>
        
        <div style='text-align: center; background: #e8f4f8; padding: 15px; border-radius: 5px;'>
            <h4>🎫 Mã vé điện tử: #{booking.Id}</h4>
            <p>Vui lòng xuất trình mã này tại quầy để nhận vé</p>
        </div>
        
        <p>Chúc bạn có những giây phút giải trí tuyệt vời!</p>
    </div>
    <div style='text-align: center; padding: 20px; color: #666; font-size: 12px;'>
        <p>© 2025 Online Movie Ticket. All rights reserved.</p>
        <p>Hotline: 1900-xxxx | Email: support@onlinemovieticket.com</p>
    </div>
</div>";

            await SendEmailAsync(email, subject, htmlContent);
        }
    }
}