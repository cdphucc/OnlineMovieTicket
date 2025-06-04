namespace OnlineMovieTicket.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public string Director { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string PosterUrl { get; set; }
        public int Duration { get; set; } // Duration in minutes
        public string Language { get; set; }
        public string Rating { get; set; } // e.g., PG-13, R
        public string Cast { get; set; } // Comma-separated list of actors
        public string TrailerUrl { get; set; } // URL to the movie trailer
        public string Status { get; set; } // e.g., "Now Showing", "Coming Soon", "Archived"
        // Navigation
        public ICollection<ShowTime> ShowTimes { get; set; }
    }
}
