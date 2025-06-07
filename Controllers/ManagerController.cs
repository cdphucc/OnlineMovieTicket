using Microsoft.AspNetCore.Mvc;
using OnlineMovieTicket.Models;
using OnlineMovieTicket.Attributes;
using OnlineMovieTicket.Data;
using Microsoft.EntityFrameworkCore;
using OnlineMovieTicket.Services;
using System.Security.Claims;

namespace OnlineMovieTicket.Controllers
{
    [AuthorizeRole(UserRole.Admin, UserRole.Manager)]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleService _roleService;

        public ManagerController(ApplicationDbContext context, IRoleService roleService)
        {
            _context = context;
            _roleService = roleService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = await _roleService.GetUserRoleAsync(userId);

            // Thống kê cho Manager Dashboard (chỉ liên quan đến cinema)
            var totalMovies = await _context.Movies.CountAsync();
            var totalShowTimes = await _context.ShowTimes.CountAsync();
            var totalBookings = await _context.Bookings.CountAsync();
            var todayBookings = await _context.Bookings
                .Where(b => b.BookingTime.Date == DateTime.Today)
                .CountAsync();

            ViewBag.TotalMovies = totalMovies;
            ViewBag.TotalShowTimes = totalShowTimes;
            ViewBag.TotalBookings = totalBookings;
            ViewBag.TodayBookings = todayBookings;
            ViewBag.UserRole = userRole;

            return View();
        }

        // Quản lý phim
        public async Task<IActionResult> Movies()
        {
            var movies = await _context.Movies
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
            return View(movies);
        }

        [HttpGet]
        public IActionResult CreateMovie()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMovie(Movie movie)
        {
            if (ModelState.IsValid)
            {
                movie.CreatedAt = DateTime.Now;
                movie.UpdatedAt = DateTime.Now;
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Phim đã được thêm thành công!";
                return RedirectToAction(nameof(Movies));
            }
            return View(movie);
        }

        [HttpGet]
        public async Task<IActionResult> EditMovie(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phim!";
                return RedirectToAction(nameof(Movies));
            }
            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMovie(int id, Movie movie)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingMovie = await _context.Movies.FindAsync(id);
                    if (existingMovie == null)
                    {
                        return NotFound();
                    }

                    existingMovie.Title = movie.Title;
                    existingMovie.Description = movie.Description;
                    existingMovie.Genre = movie.Genre;
                    existingMovie.Duration = movie.Duration;
                    existingMovie.ReleaseDate = movie.ReleaseDate;
                    existingMovie.PosterUrl = movie.PosterUrl;
                    existingMovie.TrailerUrl = movie.TrailerUrl;
                    existingMovie.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Phim đã được cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật phim!";
                }
                return RedirectToAction(nameof(Movies));
            }
            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMovie(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                // Kiểm tra xem phim có suất chiếu đang hoạt động không
                var hasActiveShowTimes = await _context.ShowTimes
                    .AnyAsync(st => st.MovieId == id && st.StartTime > DateTime.Now);

                if (hasActiveShowTimes)
                {
                    TempData["ErrorMessage"] = "Không thể xóa phim vì còn suất chiếu đang hoạt động!";
                    return RedirectToAction(nameof(Movies));
                }

                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Phim đã được xóa thành công!";
            }
            return RedirectToAction(nameof(Movies));
        }

        // Xem bookings (chỉ xem, không sửa)
        public async Task<IActionResult> Bookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.ShowTime)
                    .ThenInclude(st => st.Movie)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Seat)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(bookings);
        }

        // Quản lý suất chiếu
        public async Task<IActionResult> ShowTimes()
        {
            var showTimes = await _context.ShowTimes
                .Include(st => st.Movie)
                .Include(st => st.Room)
                .OrderBy(st => st.StartTime)
                .ToListAsync();
            return View(showTimes);
        }

        [HttpGet]
        public async Task<IActionResult> CreateShowTime()
        {
            ViewBag.Movies = await _context.Movies.ToListAsync();
            ViewBag.Rooms = await _context.Rooms.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShowTime(ShowTime showTime)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra conflict về thời gian và phòng
                var conflictingShowTime = await _context.ShowTimes
                    .AnyAsync(st => st.RoomId == showTime.RoomId &&
                                   st.StartTime.Date == showTime.StartTime.Date &&
                                   Math.Abs((st.StartTime - showTime.StartTime).TotalMinutes) < 180);

                if (conflictingShowTime)
                {
                    TempData["ErrorMessage"] = "Phòng đã có suất chiếu trong khoảng thời gian này!";
                    ViewBag.Movies = await _context.Movies.ToListAsync();
                    ViewBag.Rooms = await _context.Rooms.ToListAsync();
                    return View(showTime);
                }

                showTime.Status = "Available";
                _context.ShowTimes.Add(showTime);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Suất chiếu đã được thêm thành công!";
                return RedirectToAction(nameof(ShowTimes));
            }

            ViewBag.Movies = await _context.Movies.ToListAsync();
            ViewBag.Rooms = await _context.Rooms.ToListAsync();
            return View(showTime);
        }
    }
}