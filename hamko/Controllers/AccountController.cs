
using System.Security.Claims;
using hamko.Models;
using hamko.Service;  // DbContext namespace
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Collections.Concurrent;

namespace hamko.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private static readonly ConcurrentDictionary<string, string> OtpStore = new();
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Register(User model)
        {
            // প্রথমে ফোন/ইউজারনেম ডুপ্লিকেট আছে কিনা চেক
            var userExists = await _context.User
                .AnyAsync(u => u.UserName == model.UserName || u.Phone == model.Phone);

            if (userExists)
            {
                ModelState.AddModelError(string.Empty, "UserName or Phone already registered.");
            }

            // এখন ModelState চেক করুন (form validation + above error)
            //if (!ModelState.IsValid)
            //{
            //    return View(model);
            //}

            // Password Hash
            var passwordHasher = new PasswordHasher<User>();
            model.Password = passwordHasher.HashPassword(model, model.Password);
            model.ConfirmPassword = null;

            // Generate OTP
            var otp = GenerateOtp();
            OtpStore[model.Phone] = otp;

            // Send OTP via SMS
            SendOtpSms(model.Phone, otp);

            // Temporarily store user
            TempData["PendingUser"] = System.Text.Json.JsonSerializer.Serialize(model);

            // Redirect to OTP page
            return RedirectToAction("VerifyOtp", new { phone = model.Phone });
        }



        [HttpGet]
        public IActionResult VerifyOtp(string phone)
        {
            ViewBag.Phone = phone;
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string phone, string inputOtp)
        {
            if (!OtpStore.TryGetValue(phone, out var savedOtp))
            {
                ModelState.AddModelError("", "OTP expired or invalid. Please register again.");
                return View();
            }

            if (inputOtp == savedOtp)
            {
                if (!TempData.TryGetValue("PendingUser", out var userObj))
                {
                    ModelState.AddModelError("", "Session expired. Please register again.");
                    return View();
                }

                var userJson = userObj as string;
                var user = System.Text.Json.JsonSerializer.Deserialize<User>(userJson);

                user.IsVerified = true;
                _context.User.Add(user);
                await _context.SaveChangesAsync();

                // Remove OTP after successful verification
                OtpStore.TryRemove(phone, out _);

                TempData["Success"] = "Registration complete! Please login.";
                return RedirectToAction("Login");
            }
            else
            {
                ModelState.AddModelError("", "Invalid OTP. Please try again.");
                ViewBag.Phone = phone;
                return View();
            }
        }

        // Simple OTP generator - 6 digit numeric
        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        // Dummy SMS sender - Replace this with real SMS API integration
        private void SendOtpSms(string phone, string otp)
        {
            // Example: Call SMS Gateway API here to send SMS
            Console.WriteLine($"Sending OTP {otp} to phone {phone}");
        }

        [HttpGet]
        public IActionResult ResendOtp(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return BadRequest();

            var otp = GenerateOtp();
            OtpStore[phone] = otp;
            SendOtpSms(phone, otp);

            return Ok();
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            Random rnd = new Random();
            int num1 = rnd.Next(0, 10);
            int num2 = rnd.Next(0, 10);

            ViewBag.CaptchaNum1 = num1;
            ViewBag.CaptchaNum2 = num2;
            HttpContext.Session.SetInt32("CaptchaAnswer", num1 + num2);

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(User model, string CaptchaInput)
        {
            int? correctAnswer = HttpContext.Session.GetInt32("CaptchaAnswer");

            if (correctAnswer == null || CaptchaInput == null || !int.TryParse(CaptchaInput, out int userAnswer) || userAnswer != correctAnswer)
            {
                ModelState.AddModelError("CaptchaInput", "CAPTCHA answer is incorrect.");
            }

            //if (ModelState.IsValid)
            //{
            // UserName দিয়ে user বের করো
            var user = await _context.User.FirstOrDefaultAsync(u => u.UserName == model.UserName);
            if (user != null)
            {
                var passwordHasher = new PasswordHasher<User>();
                // password verify করো
                var result = passwordHasher.VerifyHashedPassword(user, user.Password, model.Password);
                if (result == PasswordVerificationResult.Success)
                {
                    var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim("UserId", user.Id.ToString())
                };

                    var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync("MyCookieAuth", principal);

                    TempData["SuccessMessage"] = "Welcome to Dashboard, login successful!";
                    return RedirectToAction("Index", "Dashboard");
                }
                //}

                ModelState.AddModelError(string.Empty, "Invalid username or password");
            }

            // CAPTCHA আবার তৈরি করো
            Random rnd = new Random();
            int num1 = rnd.Next(0, 10);
            int num2 = rnd.Next(0, 10);
            ViewBag.CaptchaNum1 = num1;
            ViewBag.CaptchaNum2 = num2;
            HttpContext.Session.SetInt32("CaptchaAnswer", num1 + num2);

            return View(model);
        }
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Login");
        }


    }
}
