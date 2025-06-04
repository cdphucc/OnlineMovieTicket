using OnlineMovieTicket.Models;

public class Booking
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public DateTime BookingTime { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    // Nếu cần, giữ lại payment
    public int PaymentId { get; set; }
    public ApplicationUser User { get; set; }
    public Payment Payment { get; set; }
    public ICollection<BookingDetail> BookingDetails { get; set; }
}