using Microsoft.AspNetCore.Mvc;
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

    public IActionResult Index()
    {
        // Lấy phim sắp chiếu (Coming Soon) và phim đang chiếu (Now Showing)
        var comingSoon = _context.Movies
            .Where(m => m.Status == "Coming Soon")
            .OrderBy(m => m.ReleaseDate)
            .ToList();

        var nowShowing = _context.Movies
            .Where(m => m.Status == "Now Showing")
            .OrderBy(m => m.ReleaseDate)
            .ToList();

        ViewBag.ComingSoon = comingSoon;
        ViewBag.NowShowing = nowShowing;
        return View();
    }
}