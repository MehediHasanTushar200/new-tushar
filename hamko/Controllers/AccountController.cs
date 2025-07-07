
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
        //register page//
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        //register page//

        //register page data submit//
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
        //register page data submit//

        //register datar jonno otp page//
        [HttpGet]
        public IActionResult VerifyOtp(string phone)
        {
            ViewBag.Phone = phone;
            return View();
        }
        //register datar jonno otp page//

        //register datar jonno otp page data submit//
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
        //register datar jonno otp page data submit//

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

        //otp resend er jonno//
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
        //otp resend er jonno//

        //login page er jonno//
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
        //login page er jonno//

        //login page er data submit er jonno//
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(User model, string CaptchaInput)
        {
            int? correctAnswer = HttpContext.Session.GetInt32("CaptchaAnswer");

            if (correctAnswer == null || CaptchaInput == null || !int.TryParse(CaptchaInput, out int userAnswer) || userAnswer != correctAnswer)
            {
                ModelState.AddModelError("CaptchaInput", "CAPTCHA answer is incorrect.");
                return View(model);
            }

            var user = await _context.User.FirstOrDefaultAsync(u => u.UserName == model.UserName);
            if (user != null)
            {
                var passwordHasher = new PasswordHasher<User>();
                var result = passwordHasher.VerifyHashedPassword(user, user.Password, model.Password);
                if (result == PasswordVerificationResult.Success)
                {
                    //if (!user.IsVerified)
                    //{
                    //    ModelState.AddModelError("", "Your phone number is not verified.");
                    //    return View(model);
                    //}

                    // Store username temporarily
                    TempData["LoginUserId"] = user.Id;
                    TempData["LoginUserName"] = user.UserName;

                    // Generate and send OTP
                    var otp = GenerateOtp();
                    OtpStore[user.Phone] = otp;
                    SendOtpSms(user.Phone, otp);

                    return RedirectToAction("VerifyLoginOtp", new { phone = user.Phone });
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid username or password");

            // CAPTCHA আবার তৈরি করো
            Random rnd = new Random();
            int num1 = rnd.Next(0, 10);
            int num2 = rnd.Next(0, 10);
            ViewBag.CaptchaNum1 = num1;
            ViewBag.CaptchaNum2 = num2;
            HttpContext.Session.SetInt32("CaptchaAnswer", num1 + num2);

            return View(model);
        }
        //login page er data submit er jonno//

        //login verify//

        [HttpGet]
        public IActionResult VerifyLoginOtp(string phone)
        {
            ViewBag.Phone = phone;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyLoginOtp(string phone, string inputOtp)
        {
            if (!OtpStore.TryGetValue(phone, out var savedOtp))
            {
                ModelState.AddModelError("", "OTP expired or invalid. Please try login again.");
                return View();
            }

            if (inputOtp == savedOtp)
            {
                if (!TempData.TryGetValue("LoginUserId", out var userIdObj) ||
                    !TempData.TryGetValue("LoginUserName", out var usernameObj))
                {
                    ModelState.AddModelError("", "Session expired. Please try login again.");
                    return View();
                }

                var userId = int.Parse(userIdObj.ToString());
                var username = usernameObj.ToString();

                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim("UserId", userId.ToString())
        };

                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("MyCookieAuth", principal);

                OtpStore.TryRemove(phone, out _);

                TempData["SuccessMessage"] = "Welcome! Login successful with OTP.";
                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError("", "Invalid OTP. Please try again.");
            ViewBag.Phone = phone;
            return View();
        }
        //login verify//

        [HttpGet]
        public IActionResult ResendLoginOtp(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return BadRequest();

            var otp = GenerateOtp();
            OtpStore[phone] = otp;
            SendOtpSms(phone, otp);

            return Ok();
        }




        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Login");
        }





    }
}
