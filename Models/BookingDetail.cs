namespace OnlineMovieTicket.Models
{
    public class BookingDetail
    {
        public int Id { get; set; }
        public int BookingId { get; set; } // ID of the booking this detail belongs to
        public int ShowTimeId { get; set; } // ID of the showtime for this booking detail
        public int SeatId { get; set; } // ID of the seat booked
        public decimal Price { get; set; } // Price of the ticket for this booking detail
        public string Status { get; set; } // e.g., "Booked", "Cancelled", "Pending"
        //Navigation
        public Booking Booking { get; set; } // Navigation property to Booking
        public ShowTime ShowTime { get; set; } // Navigation property to ShowTime
        public Seat Seat { get; set; } // Navigation property to Seat
    }
}
