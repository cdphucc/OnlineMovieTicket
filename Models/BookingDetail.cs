using OnlineMovieTicket.Models;


public class BookingDetail
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int ShowTimeId { get; set; }
    public int SeatId { get; set; }
    public Booking Booking { get; set; }
    public ShowTime ShowTime { get; set; }
    public Seat Seat { get; set; }
}