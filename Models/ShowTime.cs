namespace OnlineMovieTicket.Models
{
    public class ShowTime
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public int RoomId { get; set; }
        public DateTime StartTime { get; set; } // Start time of the show
        public decimal Price { get; set; } // Price of the ticket for this showtime
        public string Status { get; set; } // e.g., Active, Inactive, Sold out
        public string Format { get; set; } // e.g., 2D, 3D, IMAX
        //Navigation
        public Movie Movie { get; set; }
        public Room Room { get; set; }
        public ICollection<BookingDetail> BookingDetails { get; set; } // List of booking details for this showtime
    }
}
