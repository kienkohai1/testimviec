using System.ComponentModel.DataAnnotations;

namespace testimviec.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tên công việc")]
        public required string Title { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Kỹ năng yêu cầu")]
        public required string Skills { get; set; } // Chuỗi kỹ năng, cách nhau bởi dấu phẩy

        [Display(Name = "Số năm kinh nghiệm tối thiểu")]
        public int MinExperienceYears { get; set; }

        [Display(Name = "Địa điểm")]
        public string? Location { get; set; }

        [Display(Name = "Mức lương (tham khảo)")]
        public string? SalaryRange { get; set; }
    }
}

