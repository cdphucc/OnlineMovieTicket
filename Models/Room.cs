namespace OnlineMovieTicket.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }
        //Navigation
        public ICollection<ShowTime> ShowTimes { get; set; } // List of showtimes in this room
        public ICollection<Seat> Seats { get; set; } // List of seats in this room
    }
}