namespace OnlineMovieTicket.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CinemaId { get; set; }
        public string ScreenType { get; set; } // e.g., 2D, 3D, IMAX
        public string Status { get; set; } // e.g., Available, Unavailable
        //Navigation
        public Cinema Cinema { get; set; } // Navigation property to Cinema
        public ICollection<ShowTime> ShowTimes { get; set; } // List of showtimes in this room
        public ICollection<Seat> Seats { get; set; } // List of seats in this room
    }
}
