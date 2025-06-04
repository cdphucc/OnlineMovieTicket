namespace OnlineMovieTicket.Models
{
    public class Cinema
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Status { get; set; } //e.g Active or Inactive
        //Navigation
        public ICollection<Room> Rooms { get; set; } // List of rooms in this cinema
    }
}
