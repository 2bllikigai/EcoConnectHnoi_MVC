using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoConnect_Hanoi.Data;
using EcoConnect_Hanoi.Models;
using System.Security.Claims; // Để lấy UserId
using Microsoft.AspNetCore.Authorization;

namespace EcoConnect_Hanoi.Controllers
{
    public class GreenChallengesController : Controller
    {
        private readonly EcoConnectHnContext _context;

        public GreenChallengesController(EcoConnectHnContext context)
        {
            _context = context;
        }

        // GET: /GreenChallenges/Index
        // Hiển thị danh sách các thử thách đang hoạt động/sắp tới
        public async Task<IActionResult> Index()
        {
            var challenges = await _context.Challenges
                .OrderBy(c => c.StartDate)
                .ToListAsync();

            return View(challenges);
        }
        // GET: /GreenChallenges/Details/{id}
        // Hiển thị chi tiết một thử thách
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var challenge = await _context.Challenges
                                          .FirstOrDefaultAsync(m => m.ChallengeId == id);
            if (challenge == null) return NotFound();

            // Nếu người dùng đã đăng nhập, kiểm tra xem họ đã tham gia thử thách này chưa
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.UserChallenge = await _context.UserChallenges
                                                     .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.ChallengeId == id);
            }

            return View(challenge);
        }

        // POST: /GreenChallenges/Enroll/{id}
        // Cho phép người dùng tham gia một thử thách
        [Authorize] // Yêu cầu người dùng đăng nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var challenge = await _context.Challenges.FindAsync(id);

            if (challenge == null) return NotFound();

            // Kiểm tra xem người dùng đã tham gia thử thách này chưa
            var existingEnrollment = await _context.UserChallenges
                                                   .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.ChallengeId == id);
            if (existingEnrollment != null)
            {
                TempData["ErrorMessage"] = "Bạn đã tham gia thử thách này rồi!";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            if (challenge.Status != ChallengeStatus.Active && challenge.Status != ChallengeStatus.Upcoming)
            {
                TempData["ErrorMessage"] = "Thử thách này không thể tham gia vào lúc này.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var userChallenge = new UserChallenge
            {
                UserId = userId,
                ChallengeId = id,
                EnrollmentDate = DateTime.Now,
                Progress = 0,
                Status = UserChallengeStatus.Enrolled
            };

            _context.Add(userChallenge);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Bạn đã tham gia thử thách thành công!";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // GET: /GreenChallenges/MyChallenges
        // Hiển thị các thử thách mà người dùng đã tham gia
        [Authorize]
        public async Task<IActionResult> MyChallenges()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var myChallenges = await _context.UserChallenges
                                             .Include(uc => uc.Challenge) // Load thông tin thử thách liên quan
                                             .Where(uc => uc.UserId == userId)
                                             .OrderByDescending(uc => uc.EnrollmentDate)
                                             .ToListAsync();
            return View(myChallenges);
        }

        // POST: /GreenChallenges/UpdateProgress/{id}
        // Cập nhật tiến độ của một thử thách đang tham gia
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProgress(int id, int progress)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userChallenge = await _context.UserChallenges
                                              .Include(uc => uc.Challenge)
                                              .FirstOrDefaultAsync(uc => uc.UserChallengeId == id && uc.UserId == userId);

            if (userChallenge == null) return NotFound();

            if (userChallenge.Status == UserChallengeStatus.Completed || userChallenge.Status == UserChallengeStatus.Failed)
            {
                TempData["ErrorMessage"] = "Thử thách này đã hoàn thành hoặc thất bại, không thể cập nhật tiến độ.";
                return RedirectToAction(nameof(Details), new { id = userChallenge.ChallengeId });
            }

            userChallenge.Progress = Math.Clamp(progress, 0, 100);

            if (userChallenge.Progress == 100)
            {
                userChallenge.CompletionDate = DateTime.Now;
                userChallenge.Status = UserChallengeStatus.Completed;

                // Tăng EcoPoints cho người dùng
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.EcoPoints += userChallenge.Challenge.RewardPoints;
                    _context.Update(user);
                }
                TempData["SuccessMessage"] = $"Chúc mừng! Bạn đã hoàn thành thử thách '{userChallenge.Challenge.Title}' và nhận được {userChallenge.Challenge.RewardPoints} EcoPoints!";
            }
            else
            {
                userChallenge.Status = UserChallengeStatus.InProgress;
                TempData["InfoMessage"] = "Tiến độ thử thách đã được cập nhật.";
            }

            _context.Update(userChallenge);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = userChallenge.ChallengeId });
        }
       
        

    }
}