
using System.ComponentModel.DataAnnotations;

namespace NguyenHoangPhuoc_AttendanceAPI.Models
{
    public class IncidentAlert
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string ChildUserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string IncidentType { get; set; } = "PossibleConflict";

        public double? SoundLevelDb { get; set; }

        public double? MotionIntensity { get; set; }

        public double? AudioConfidence { get; set; }

        [Required]
        [MaxLength(20)]
        public string RiskLevel { get; set; } = "High";

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public double? LocationAccuracyMeters { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "New";

        public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ParentNotifiedAtUtc { get; set; }
    }
}
