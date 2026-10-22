
using System.ComponentModel.DataAnnotations;

namespace NguyenHoangPhuoc_AttendanceAPI.DTOs
{
    public class CreateIncidentAlertRequest
    {
        [Required]
        [MaxLength(50)]
        public string IncidentType { get; set; } = "PossibleConflict";

        [Range(0, 200)]
        public double? SoundLevelDb { get; set; }

        [Range(0, 1000)]
        public double? MotionIntensity { get; set; }

        [Range(0, 1)]
        public double? AudioConfidence { get; set; }

        [Required]
        [MaxLength(20)]
        public string RiskLevel { get; set; } = "High";

        [Range(-90, 90)]
        public double? Latitude { get; set; }

        [Range(-180, 180)]
        public double? Longitude { get; set; }

        [Range(0, 100000)]
        public double? LocationAccuracyMeters { get; set; }
    }
}
