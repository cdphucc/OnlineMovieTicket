using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineMovieTicket.Data;
using OnlineMovieTicket.Models;
using System.Linq;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(string tab = "nowshowing")
    {
        IQueryable<Movie> moviesQuery;
        if (tab == "comingsoon")
        {
            // Phim sắp chiếu: Status = "ComingSoon"
            moviesQuery = _context.Movies
                .Where(m => m.Status == "ComingSoon");
            ViewBag.Tab = "comingsoon";
        }
        else
        {
            // Phim đang chiếu: Status = "NowShowing" và có suất chiếu khả dụng
            moviesQuery = _context.Movies
                .Include(m => m.ShowTimes)
                .Where(m => m.Status == "NowShowing" && m.ShowTimes.Any(st => st.Status == "Available" && st.StartTime > DateTime.Now));
            ViewBag.Tab = "nowshowing";
        }

        var movies = moviesQuery
            .Include(m => m.ShowTimes)
            .OrderBy(m => m.Title)
            .ToList();

        return View(movies);
    }
}