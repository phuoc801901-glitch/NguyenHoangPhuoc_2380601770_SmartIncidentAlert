
using Microsoft.AspNetCore.Identity;

namespace NguyenHoangPhuoc_AttendanceAPI.Models
{
    public class User : IdentityUser
    {
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
