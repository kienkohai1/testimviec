using System.ComponentModel.DataAnnotations;

namespace testimviec.Models
{
    public class Candidate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Họ và Tên")]
        public string FullName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string Phone { get; set; }

        [Display(Name = "Kỹ năng")]
        public string Skills { get; set; } // Lưu dạng chuỗi cách nhau bởi dấu phẩy

        [Display(Name = "Số năm kinh nghiệm")]
        public int ExperienceYears { get; set; }

        [Display(Name = "Tóm tắt ứng viên")]
        public string Summary { get; set; }

        public string FilePath { get; set; } // Đường dẫn lưu file CV gốc

        public DateTime AnalyzedAt { get; set; } = DateTime.Now;

        // Lưu toàn bộ JSON từ Gemini để dự phòng hoặc tái phân tích
        public string RawJsonResult { get; set; }
    }
}