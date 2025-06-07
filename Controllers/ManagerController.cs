using Microsoft.AspNetCore.Mvc;
using OnlineMovieTicket.Models;
using OnlineMovieTicket.Attributes;
using OnlineMovieTicket.Data;
using Microsoft.EntityFrameworkCore;

namespace OnlineMovieTicket.Controllers
{
    [AuthorizeRole(UserRole.Admin, UserRole.Manager)]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Movies()
        {
            var movies = await _context.Movies.ToListAsync();
            return View(movies);
        }

        public async Task<IActionResult> Bookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.ShowTime)
                    .ThenInclude(st => st.Movie)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(bookings);
        }

        public async Task<IActionResult> ShowTimes()
        {
            var showTimes = await _context.ShowTimes
                .Include(st => st.Movie)
                .Include(st => st.Room)
                .OrderBy(st => st.StartTime)
                .ToListAsync();
            return View(showTimes);
        }

        // Add CRUD operations for movies, showtimes, etc.
    }
}