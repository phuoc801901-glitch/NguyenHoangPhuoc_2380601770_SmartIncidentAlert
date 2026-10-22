using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NguyenHoangPhuoc_AttendanceAPI.DTOs;
using NguyenHoangPhuoc_AttendanceAPI.Models;
using NguyenHoangPhuoc_AttendanceAPI.Services;

namespace NguyenHoangPhuoc_AttendanceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly EmailOtpService _emailOtpService;

        private static readonly ConcurrentDictionary<string, OtpEntry>
            PendingOtps = new();

        private sealed class OtpEntry
        {
            public required string CodeHash { get; init; }
            public required DateTime ExpiresAtUtc { get; init; }
            public int Attempts { get; set; }
        }

        public AuthController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            EmailOtpService emailOtpService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _emailOtpService = emailOtpService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "Username, email và password không được để trống."
                });
            }

            var existingUser =
                await _userManager.FindByNameAsync(request.UserName);

            if (existingUser != null)
                return BadRequest(new { message = "Username đã tồn tại." });

            var existingEmail =
                await _userManager.FindByEmailAsync(request.Email);

            if (existingEmail != null)
                return BadRequest(new { message = "Email đã được sử dụng." });

            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email,
                FullName = request.FullName,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            if (await _roleManager.RoleExistsAsync("Employee"))
                await _userManager.AddToRoleAsync(user, "Employee");

            return Ok(new { message = "Đăng ký thành công." });
        }

        // Bước 1: Kiểm tra mật khẩu và gửi OTP, KHÔNG cấp JWT.
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.UserName);

            if (user == null || !user.IsActive ||
                !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return Unauthorized(new
                {
                    message = "Tên đăng nhập hoặc mật khẩu không đúng."
                });
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return BadRequest(new
                {
                    message = "Tài khoản chưa có email để nhận OTP."
                });
            }

            var now = DateTime.UtcNow;

            if (PendingOtps.TryGetValue(user.Id, out var previous) &&
                previous.ExpiresAtUtc > now &&
                previous.Attempts < 5)
            {
                return StatusCode(429, new
                {
                    message = "OTP đang còn hiệu lực. Hãy kiểm tra email trước khi yêu cầu mã mới."
                });
            }

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000)
                .ToString("D6");

            var entry = new OtpEntry
            {
                CodeHash = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(code))),
                ExpiresAtUtc = now.AddMinutes(5),
                Attempts = 0
            };

            PendingOtps[user.Id] = entry;

            try
            {
                await _emailOtpService.SendOtpAsync(user.Email, code);
            }
            catch (Exception ex)
            {
                PendingOtps.TryRemove(user.Id, out _);

                // Chi tiet loi chi ghi server-side, khong tra thong tin SMTP cho client.
                Console.Error.WriteLine($"OTP email send failed: {ex.Message}");

                return StatusCode(500, new
                {
                    message = "Không gửi được OTP. Hãy kiểm tra cấu hình email của backend."
                });
            }

            return Ok(new
            {
                message = "OTP đã được gửi tới email đăng ký.",
                userName = user.UserName,
                expiresInSeconds = 300
            });
        }

        // Bước 2: Xác minh OTP rồi mới cấp JWT.
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserName) ||
                string.IsNullOrWhiteSpace(request.Otp) ||
                request.Otp.Length != 6 ||
                !request.Otp.All(char.IsDigit))
            {
                return BadRequest(new { message = "Vui lòng nhập OTP gồm 6 chữ số." });
            }

            var user = await _userManager.FindByNameAsync(request.UserName);

            if (user == null || !user.IsActive ||
                !PendingOtps.TryGetValue(user.Id, out var entry))
            {
                return Unauthorized(new
                {
                    message = "OTP không hợp lệ hoặc đã hết hạn."
                });
            }

            if (entry.ExpiresAtUtc <= DateTime.UtcNow || entry.Attempts >= 5)
            {
                PendingOtps.TryRemove(user.Id, out _);
                return Unauthorized(new
                {
                    message = "OTP đã hết hạn hoặc vượt quá số lần thử."
                });
            }

            var suppliedHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(request.Otp)));

            if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(suppliedHash),
                Convert.FromHexString(entry.CodeHash)))
            {
                entry.Attempts++;

                if (entry.Attempts >= 5)
                    PendingOtps.TryRemove(user.Id, out _);

                return Unauthorized(new
                {
                    message = "OTP không đúng."
                });
            }

            // Xóa OTP trước khi cấp token để không thể dùng lại.
            if (!PendingOtps.TryRemove(user.Id, out _))
            {
                return Unauthorized(new
                {
                    message = "OTP đã được sử dụng hoặc không còn hiệu lực."
                });
            }

            var keyString = _configuration["Jwt:SecretKey"];

            if (string.IsNullOrWhiteSpace(keyString) ||
                Encoding.UTF8.GetByteCount(keyString) < 32)
            {
                return StatusCode(500, new
                {
                    message = "JWT SecretKey chưa được cấu hình hợp lệ."
                });
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? ""),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var roles = await _userManager.GetRolesAsync(user);

            claims.AddRange(roles.Select(role =>
                new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(keyString));

            var credentials = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return Ok(new
            {
                accessToken = new JwtSecurityTokenHandler().WriteToken(token),
                expiresAtUtc = token.ValidTo,
                userName = user.UserName,
                roles
            });
        }
    }

    public sealed class VerifyOtpRequest
    {
        public string UserName { get; set; } = "";
        public string Otp { get; set; } = "";
    }
}
