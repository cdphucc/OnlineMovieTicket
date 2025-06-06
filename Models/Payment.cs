using OnlineMovieTicket.Models;

namespace OnlineMovieTicket.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }

        // ✅ CHỈ VietQR
        public string PaymentMethod { get; set; } = "VietQR"; // Luôn là VietQR
        public string TransactionId { get; set; } = "";
        public string Status { get; set; } = "Completed";
        public DateTime PaymentDate { get; set; }

        // VietQR specific fields
        public string QRCode { get; set; } = "";
        public string BankAccountNo { get; set; } = "";
        public string BankName { get; set; } = "";

        // Navigation property
        public Booking Booking { get; set; }
    }
}