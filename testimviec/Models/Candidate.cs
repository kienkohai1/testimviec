using System.ComponentModel.DataAnnotations;

namespace testimviec.Models
{
    public class Candidate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Họ và Tên")]
        public required string FullName { get; set; }

        [EmailAddress]
        public required string Email { get; set; }

        public required string Phone { get; set; }

        [Display(Name = "Kỹ năng")]
        public required string Skills { get; set; } // Lưu dạng chuỗi cách nhau bởi dấu phẩy

        [Display(Name = "Số năm kinh nghiệm")]
        public int ExperienceYears { get; set; }

        [Display(Name = "Điểm mạnh")]
        public required string Strengths { get; set; }

        [Display(Name = "Điểm yếu")]
        public required string Weaknesses { get; set; }

        [Display(Name = "Tóm tắt ứng viên")]
        public required string Summary { get; set; }

        public required string FilePath { get; set; } // Đường dẫn lưu file CV gốc

        public DateTime AnalyzedAt { get; set; } = DateTime.Now;

        // Lưu toàn bộ JSON từ AI để dự phòng hoặc tái phân tích
        public required string RawJsonResult { get; set; }
    }
}