using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using OnlineMovieTicket.Models;
using OnlineMovieTicket.Models.ViewModels;
using OnlineMovieTicket.Services;
using OnlineMovieTicket.Attributes;
using Microsoft.EntityFrameworkCore;
using OnlineMovieTicket.Data;
using SendGrid.Helpers.Mail;

namespace OnlineMovieTicket.Controllers
{
    [AuthorizeRole(UserRole.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRoleService _roleService;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<ApplicationUser> userManager, IRoleService roleService, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleService = roleService;
            _context = context;
        }

        // Admin Dashboard
        public async Task<IActionResult> Index()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalMovies = await _context.Movies.CountAsync();
            var totalBookings = await _context.Bookings.CountAsync();
            var totalRevenue = await _context.Payments
                .Where(p => p.Status == "Completed")
                .SumAsync(p => p.Amount);
            var todayBookings = await _context.Bookings
                .Where(b => b.BookingTime.Date == DateTime.Today)
                .CountAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalMovies = totalMovies;
            ViewBag.TotalBookings = totalBookings;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TodayBookings = todayBookings;

            return View();
        }

        // User Management List
        public async Task<IActionResult> UserManagement()
        {
            try
            {
                var users = await _userManager.Users
                    .OrderBy(u => u.Role)
                    .ThenBy(u => u.FullName)
                    .ToListAsync();
                return View(users);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading users.";
                return View(new List<ApplicationUser>());
            }
        }

        // GET: Edit User
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Invalid user ID.";
                return RedirectToAction(nameof(UserManagement));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(UserManagement));
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                Role = user.Role,
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                CreatedBy = user.CreatedBy,
                UpdatedBy = user.UpdatedBy
            };

            // Prepare ViewBag data
            ViewBag.Roles = Enum.GetValues<UserRole>().Cast<UserRole>().ToList();
            ViewBag.Genders = new List<string> { "Male", "Female", "Other" };

            return View(model);
        }

        // POST: Edit User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, string fullName, string phoneNumber,
            string gender, DateTime dateOfBirth, string address, UserRole role, bool emailConfirmed = false)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Invalid user ID.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // Basic validation
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    TempData["ErrorMessage"] = "Full Name is required.";
                    return RedirectToAction(nameof(EditUser), new { id });
                }

                if (string.IsNullOrWhiteSpace(gender))
                {
                    TempData["ErrorMessage"] = "Gender is required.";
                    return RedirectToAction(nameof(EditUser), new { id });
                }

                if (dateOfBirth == default(DateTime))
                {
                    TempData["ErrorMessage"] = "Date of Birth is required.";
                    return RedirectToAction(nameof(EditUser), new { id });
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // Prevent admin from changing their own role to non-admin
                var currentUserId = _userManager.GetUserId(User);
                if (user.Id == currentUserId && role != UserRole.Admin)
                {
                    TempData["ErrorMessage"] = "You cannot change your own role from Admin.";
                    return RedirectToAction(nameof(EditUser), new { id });
                }

                // Store original role for comparison
                var originalRole = user.Role;

                // Update user properties (Email is not changeable)
                user.FullName = fullName.Trim();
                user.PhoneNumber = phoneNumber?.Trim();
                user.Gender = gender;
                user.DateOfBirth = dateOfBirth;
                user.Address = address?.Trim();
                user.EmailConfirmed = emailConfirmed;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = currentUserId;

                // Update user in database
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Failed to update user: {errors}";
                    return RedirectToAction(nameof(EditUser), new { id });
                }

                // Update role if changed
                if (originalRole != role)
                {
                    var roleSuccess = await _roleService.AssignRoleToUserAsync(user.Id, role, currentUserId);
                    if (!roleSuccess)
                    {
                        TempData["ErrorMessage"] = "User information updated but failed to update role.";
                        return RedirectToAction(nameof(EditUser), new { id });
                    }
                }

                TempData["SuccessMessage"] = $"User '{user.FullName}' updated successfully.";
                return RedirectToAction(nameof(UserManagement));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the user.";
                return RedirectToAction(nameof(EditUser), new { id });
            }
        }

        // POST: Reset Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Invalid user ID.";
                    return RedirectToAction(nameof(UserManagement));
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // Generate new random password
                var newPassword = GenerateRandomPassword();
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (result.Succeeded)
                {
                    TempData["ResetPasswordSuccess"] = $"Password reset successfully. New password: {newPassword}";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Failed to reset password: {errors}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while resetting password.";
            }

            return RedirectToAction(nameof(EditUser), new { id });
        }

        // POST: Delete User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Invalid user ID.";
                    return RedirectToAction(nameof(UserManagement));
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // Prevent admin from deleting themselves
                var currentUserId = _userManager.GetUserId(User);
                if (user.Id == currentUserId)
                {
                    TempData["ErrorMessage"] = "You cannot delete your own account.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // Prevent deleting other admins for security
                if (user.Role == UserRole.Admin)
                {
                    TempData["ErrorMessage"] = "Cannot delete admin users for security reasons.";
                    return RedirectToAction(nameof(UserManagement));
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = $"User '{user.FullName}' deleted successfully.";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Failed to delete user: {errors}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the user.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        // Helper method to generate random password
        private string GenerateRandomPassword()
        {
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";

            var random = new Random();
            var password = new char[8];

            // Ensure at least one character from each category
            password[0] = lowercase[random.Next(lowercase.Length)];
            password[1] = uppercase[random.Next(uppercase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = special[random.Next(special.Length)];

            // Fill the rest with random characters from all categories
            var allChars = lowercase + uppercase + digits + special;
            for (int i = 4; i < 8; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }

            // Shuffle the array
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                char temp = password[i];
                password[i] = password[j];
                password[j] = temp;
            }

            return new string(password);
        }
        public async Task<IActionResult> MovieManagement()
        {
            var movies = await _context.Movies
                .Include(m => m.ShowTimes)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
            return View(movies);
        }

        [HttpGet]
        public IActionResult CreateMovie()
        {
            ViewBag.Genres = new List<string>
            {
                "Action", "Comedy", "Drama", "Horror", "Romance",
                "Sci-Fi", "Thriller", "Animation", "Documentary", "Adventure"
            };
            ViewBag.Statuses = new List<string> { "NowShowing", "ComingSoon", "Ended" };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMovie(Movie movie, IFormFile? posterFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload poster nếu có
                    if (posterFile != null && posterFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "posters");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + posterFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await posterFile.CopyToAsync(fileStream);
                        }

                        movie.PosterUrl = "/images/posters/" + uniqueFileName;
                    }

                    movie.CreatedAt = DateTime.Now;
                    movie.UpdatedAt = DateTime.Now;
                    movie.Status = movie.Status ?? "ComingSoon";

                    _context.Movies.Add(movie);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Phim đã được thêm thành công!";
                    return RedirectToAction(nameof(MovieManagement));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm phim!";
                }
            }

            ViewBag.Genres = new List<string>
            {
                "Action", "Comedy", "Drama", "Horror", "Romance",
                "Sci-Fi", "Thriller", "Animation", "Documentary", "Adventure"
            };
            ViewBag.Statuses = new List<string> { "NowShowing", "ComingSoon", "Ended" };
            return View(movie);
        }

        [HttpGet]
        public async Task<IActionResult> EditMovie(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy phim!";
                return RedirectToAction(nameof(MovieManagement));
            }

            ViewBag.Genres = new List<string>
            {
                "Action", "Comedy", "Drama", "Horror", "Romance",
                "Sci-Fi", "Thriller", "Animation", "Documentary", "Adventure"
            };
            ViewBag.Statuses = new List<string> { "NowShowing", "ComingSoon", "Ended" };
            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMovie(int id, Movie movie, IFormFile? posterFile)
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

                    // Xử lý upload poster mới nếu có
                    if (posterFile != null && posterFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "posters");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + posterFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await posterFile.CopyToAsync(fileStream);
                        }

                        movie.PosterUrl = "/images/posters/" + uniqueFileName;
                    }
                    else
                    {
                        movie.PosterUrl = existingMovie.PosterUrl;
                    }

                    existingMovie.Title = movie.Title;
                    existingMovie.Description = movie.Description;
                    existingMovie.Genre = movie.Genre;
                    existingMovie.Duration = movie.Duration;
                    existingMovie.ReleaseDate = movie.ReleaseDate;
                    existingMovie.PosterUrl = movie.PosterUrl;
                    existingMovie.TrailerUrl = movie.TrailerUrl;
                    existingMovie.Status = movie.Status;
                    existingMovie.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Phim đã được cập nhật thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật phim!";
                }
                return RedirectToAction(nameof(MovieManagement));
            }

            ViewBag.Genres = new List<string>
            {
                "Action", "Comedy", "Drama", "Horror", "Romance",
                "Sci-Fi", "Thriller", "Animation", "Documentary", "Adventure"
            };
            ViewBag.Statuses = new List<string> { "NowShowing", "ComingSoon", "Ended" };
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
                    return RedirectToAction(nameof(MovieManagement));
                }

                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Phim đã được xóa thành công!";
            }
            return RedirectToAction(nameof(MovieManagement));
        }

        // QUẢN LÝ BOOKING
        public async Task<IActionResult> BookingManagement()
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking != null && booking.Status != "Cancelled")
            {
                booking.Status = "Cancelled";

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã hủy booking thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy booking này!";
            }

            return RedirectToAction(nameof(BookingManagement));
        }

        // QUẢN LÝ LỊCH CHIẾU
        public async Task<IActionResult> ShowTimeManagement()
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
            ViewBag.Movies = await _context.Movies
                .Where(m => m.Status == "NowShowing" || m.Status == "ComingSoon")
                .ToListAsync();
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
                var movie = await _context.Movies.FindAsync(showTime.MovieId);
                var endTime = showTime.StartTime.AddMinutes(movie?.Duration ?? 120);

                var conflictingShowTime = await _context.ShowTimes
                    .AnyAsync(st => st.RoomId == showTime.RoomId &&
                                   st.StartTime.Date == showTime.StartTime.Date &&
                                   ((st.StartTime <= showTime.StartTime && st.StartTime.AddMinutes(st.Movie.Duration + 30) > showTime.StartTime) ||
                                    (showTime.StartTime <= st.StartTime && endTime.AddMinutes(30) > st.StartTime)));

                if (conflictingShowTime)
                {
                    TempData["ErrorMessage"] = "Phòng đã có suất chiếu trong khoảng thời gian này (bao gồm 30 phút dọn dẹp)!";
                    ViewBag.Movies = await _context.Movies
                        .Where(m => m.Status == "NowShowing" || m.Status == "ComingSoon")
                        .ToListAsync();
                    ViewBag.Rooms = await _context.Rooms.ToListAsync();
                    return View(showTime);
                }

                showTime.Status = "Available";
                _context.ShowTimes.Add(showTime);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Suất chiếu đã được thêm thành công!";
                return RedirectToAction(nameof(ShowTimeManagement));
            }

            ViewBag.Movies = await _context.Movies
                .Where(m => m.Status == "NowShowing" || m.Status == "ComingSoon")
                .ToListAsync();
            ViewBag.Rooms = await _context.Rooms.ToListAsync();
            return View(showTime);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShowTime(int id)
        {
            var showTime = await _context.ShowTimes.FindAsync(id);
            if (showTime != null)
            {
                // Kiểm tra xem có booking nào không
                var hasBookings = await _context.BookingDetails
                    .AnyAsync(bd => bd.ShowTimeId == id);

                if (hasBookings)
                {
                    TempData["ErrorMessage"] = "Không thể xóa suất chiếu vì đã có người đặt vé!";
                    return RedirectToAction(nameof(ShowTimeManagement));
                }

                _context.ShowTimes.Remove(showTime);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Suất chiếu đã được xóa thành công!";
            }
            return RedirectToAction(nameof(ShowTimeManagement));
        }


    }
}