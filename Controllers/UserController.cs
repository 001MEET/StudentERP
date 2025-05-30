﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using StudentERP.Models;
using MailKit.Net.Smtp;
using StudentERP.Repository.IRepository;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace StudentERP.Controllers
{
    public class UserController : Controller
    {
        private readonly IStudentLoginRepository _studentLoginRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly SmtpSettings _smtpSettings;

        public UserController(IStudentLoginRepository studentLoginRepository, IWebHostEnvironment environment, IConfiguration configuration, IOptions<SmtpSettings> smtpSettings)
        {
            _studentLoginRepository = studentLoginRepository;
            _environment = environment;
            _configuration = configuration;
            _smtpSettings = smtpSettings.Value;
        }

        public async Task<IActionResult> Dashboard()
        {
            var email = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var student = await _studentLoginRepository.GetStudentByEmail(email);
            if (student == null)
            {
                return RedirectToAction("Login");
            }

            var profile = await _studentLoginRepository.GetStudentProfile(student.StudentId);
            var personalDetails = await _studentLoginRepository.GetPersonalDetails(student.StudentId); // Assume this method exists
            var contactDetails = await _studentLoginRepository.GetContactDetails(student.StudentId); // Assume this method exists
            var parentsDetails = await _studentLoginRepository.GetParentsDetails(student.StudentId); // Assume this method exists

            var viewModel = new DashboardViewModel
            {
                StudentEmail = email,
                ProfilePictureName = profile?.ProfilePictureName,
                FullName = personalDetails != null ? $"{personalDetails.FirstName} {personalDetails.LastName}".Trim() : "No Data Available",
                PhoneNumber = contactDetails?.PhoneNumber ?? "No Data Available",
                FatherName = parentsDetails?.FatherName ?? "No Data Available",
                ParentPhoneNumber = parentsDetails?.ParentPhoneNumber ?? "No Data Available"
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(StudentLogin model, string fullname)
        {
            //if (!ModelState.IsValid)
            //    return Json(new { success = false, message = "Validation failed" });    // to be fixed...(tilak)

            var existingStudent = await _studentLoginRepository.GetStudentByEmail(model.StudentMail);

            if (existingStudent != null)
                return Json(new { success = false, message = "Email already registered" });

            var studentLogin = new StudentLogin
            {
                StudentMail = model.StudentMail,
                HashPassword = model.HashPassword
            };

           

           

            var result = await _studentLoginRepository.RegisterStudent(studentLogin);
            await SendWelcomeEmail(studentLogin.StudentMail, fullname);
            if (result)
                return Json(new { success = true });

            return Json(new { success = false, message = "Registration failed. Please try again." });
        }
        private async Task SendWelcomeEmail(string toEmail, string fullName)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
            message.To.Add(new MailboxAddress(fullName, toEmail));
            message.Subject = "Welcome to StudentERP!";

            message.Body = new TextPart("html")
            {
                Text = $"<h2>Hello {fullName},</h2>" +
                       "<p>Welcome to StudentERP! Your account has been successfully created.</p>" +
                       "<p>You can now log in using your email and password to access your dashboard.</p>" +
                       "<p>Best regards,<br>StudentERP Team</p>"
            };

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    // Log the error (you might want to use ILogger here)
                    Console.WriteLine($"Failed to send email: {ex.Message}");
                    // Optionally rethrow or handle silently
                }
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("StudentEmail")))
                return RedirectToAction("Dashboard");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(StudentLogin model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Validation failed" });

            var student = await _studentLoginRepository.GetStudentByEmail(model.StudentMail);
            if (student == null || student.HashPassword != _studentLoginRepository.HashPassword(model.HashPassword) || !student.IsActive)
                return Json(new { success = false, message = "Invalid email, password, or inactive account" });

            HttpContext.Session.SetString("StudentEmail", student.StudentMail);
            HttpContext.Session.SetString("StudentId", student.StudentId.ToString());
            var token = GenerateJwtToken(student);
            return Json(new { success = true, token = token });
        }

        private string GenerateJwtToken(StudentLogin student)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, student.StudentMail),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("StudentId", student.StudentId.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("StudentEmail")))
                return RedirectToAction("Login");

            var email = HttpContext.Session.GetString("StudentEmail");
            var student = await _studentLoginRepository.GetStudentByEmail(email);
            var profile = await _studentLoginRepository.GetStudentProfile(student.StudentId);

            var viewModel = new ProfileViewModel
            {
                StudentEmail = email,
                HasProfilePicture = profile != null && !string.IsNullOrEmpty(profile.ProfilePictureName)
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string CurrentPasswordConfirm, string NewPassword)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("StudentEmail")))
                return Json(new { success = false, message = "You must be logged in to change your password." });

            if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(CurrentPasswordConfirm) || string.IsNullOrEmpty(NewPassword))
                return Json(new { success = false, message = "All fields are required." });

            if (CurrentPassword != CurrentPasswordConfirm)
                return Json(new { success = false, message = "Current passwords do not match." });

            var email = HttpContext.Session.GetString("StudentEmail");
            var student = await _studentLoginRepository.GetStudentByEmail(email);
            if (student == null || student.HashPassword != _studentLoginRepository.HashPassword(CurrentPassword))
                return Json(new { success = false, message = "Current password is incorrect." });

            if (_studentLoginRepository.HashPassword(NewPassword) == student.HashPassword)
                return Json(new { success = false, message = "New password must be different from the current password." });

            student.HashPassword = _studentLoginRepository.HashPassword(NewPassword);
            var result = await _studentLoginRepository.UpdateStudent(student);
            if (result)
            {
                TempData["Success"] = "Password updated successfully!";
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Failed to update password. Please try again." });
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("StudentEmail")))
                return RedirectToAction("Login");

            var email = HttpContext.Session.GetString("StudentEmail");
            var student = await _studentLoginRepository.GetStudentByEmail(email);
            if (student == null)
                return RedirectToAction("Login");

            var viewModel = new EditProfileViewModel
            {
                StudentEmail = email,
                StudentId = student.StudentId
            };
            return View(viewModel);
        }


        // Add this new GET endpoint
        [HttpGet]
        public async Task<IActionResult> CheckProfilePicture()
        {
            var email = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { exists = false, message = "Not logged in" });
            }

            var student = await _studentLoginRepository.GetStudentByEmail(email);
            if (student == null)
            {
                return Json(new { exists = false, message = "Invalid session" });
            }

            var profile = await _studentLoginRepository.GetStudentProfile(student.StudentId);
            if (profile != null && !string.IsNullOrEmpty(profile.ProfilePictureName))
            {
                var filePath = Path.Combine(_environment.WebRootPath, "Images", profile.ProfilePictureName);
                if (System.IO.File.Exists(filePath))
                {
                    return Json(new { exists = true, fileName = profile.ProfilePictureName });
                }
            }

            return Json(new { exists = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(IFormFile profilePicture, bool replaceConfirmed = false)
        {
            Console.WriteLine("Entering EditProfile POST - Before anything else");
            try
            {
                Console.WriteLine("EditProfile POST started - Inside try block");

                var email = HttpContext.Session.GetString("StudentEmail");
                Console.WriteLine($"Session email retrieved: {email ?? "null"}");
                if (string.IsNullOrEmpty(email))
                {
                    Console.WriteLine("No email in session");
                    return Json(new { success = false, message = "You must be logged in." });
                }

                Console.WriteLine($"Fetching student for email: {email}");
                var student = await _studentLoginRepository.GetStudentByEmail(email);
                if (student == null)
                {
                    Console.WriteLine("Student not found");
                    return Json(new { success = false, message = "Invalid session." });
                }

                Console.WriteLine($"Profile picture: {profilePicture?.FileName ?? "null"}");
                if (profilePicture == null || profilePicture.Length == 0)
                {
                    Console.WriteLine("No file selected");
                    return Json(new { success = false, message = "Please select a file." });
                }

                
                var expectedFileName = $"{student.StudentId.ToString().ToUpper()}.jpg";
                if (profilePicture.FileName != expectedFileName)
                {
                    Console.WriteLine($"Invalid filename. Expected: {expectedFileName}, Got: {profilePicture.FileName}");
                    return Json(new { success = false, message = $"Invalid filename. Please rename your file to '{expectedFileName}' and try again." });
                }

                var fileName = expectedFileName;  
                var filePath = Path.Combine(_environment.WebRootPath, "Images", fileName);

                
                var existingProfile = await _studentLoginRepository.GetStudentProfile(student.StudentId);
                if (existingProfile != null && !string.IsNullOrEmpty(existingProfile.ProfilePictureName) && !replaceConfirmed)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        Console.WriteLine("Profile picture already exists, awaiting confirmation");
                        return Json(new { success = false, confirmRequired = true, message = $"A profile picture ({fileName}) already exists. Do you want to replace it?" });
                    }
                }

                
                Console.WriteLine($"WebRootPath: {_environment.WebRootPath}");
                Console.WriteLine($"Saving file to: {filePath}");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    Console.WriteLine("Writing file stream");
                    await profilePicture.CopyToAsync(fileStream);
                }

                // Save profile data to database
                var profile = new StudentProfile
                {
                    StudentId = student.StudentId,
                    ProfilePictureName = fileName
                };
                Console.WriteLine($"Saving profile to database: StudentId={profile.StudentId}, FileName={profile.ProfilePictureName}");
                var saveResult = await _studentLoginRepository.SaveOrReplaceStudentProfile(profile);
                if (!saveResult)
                {
                    Console.WriteLine("Failed to save profile to database");
                    return Json(new { success = false, message = "Failed to save profile data." });
                }

                Console.WriteLine("File and profile saved successfully");
                return Json(new { success = true, message = "Profile picture uploaded and saved successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Upload error: {ex.Message}" });
            }
        }
        //[HttpPost]
        //public async Task<IActionResult> Login(string email, string password)
        //{
        //    var student = await _studentLoginRepository.GetStudentByEmail(email);

        //    if (student == null || student.HashPassword != _studentLoginRepository.HashPassword(password))
        //    {
        //        await _studentLoginRepository.SendLoginNotificationEmail(email, false);
        //        ViewBag.Error = "Invalid email or password.";
        //        return View();
        //    }

        //    HttpContext.Session.SetString("StudentEmail", student.StudentMail);
        //    await _studentLoginRepository.SendLoginNotificationEmail(email, true);

        //    return RedirectToAction("Dashboard", "Student");
        //}


        
    }
    public class DashboardViewModel
    {
        public string StudentEmail { get; set; }
        public string ProfilePictureName { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string FatherName { get; set; }
        public string ParentPhoneNumber { get; set; }
    }

    public class ProfileViewModel
    {
        public string StudentEmail { get; set; } = null!;
        public bool HasProfilePicture { get; set; }
    }

    public class EditProfileViewModel
    {
        public string StudentEmail { get; set; } = null!;
        public Guid StudentId { get; set; }
    }




}



