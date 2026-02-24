using System.ComponentModel.DataAnnotations;

namespace testimviec.Models
{
    public class Skill
    {
        [Key]
        public int SkillId { get; set; }

        [Required]
        [Display(Name = "Tên kỹ năng")]
        public required string Name { get; set; } // Ví dụ: C#, Python, Teamwork
    }
}