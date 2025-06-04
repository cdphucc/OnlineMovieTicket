namespace OnlineMovieTicket.Models
{
    public class Seat
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public string SeatNumber { get; set; }
        public string Row { get; set; }
        public int Column { get; set; }
        public string Status { get; set; } // e.g., Available, Booked, Reserved
        //Navigation
        public Room Room { get; set; } // Navigation property to Room
        public ICollection<BookingDetail> BookingDetails { get; set; } // List of booking details for this seat
    }
}