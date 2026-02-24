using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testimviec.Models
{
    // Bảng nối giữa Công việc và Kỹ năng (Job - Skill)
    public class JobSkill
    {
        public int JobId { get; set; }
        [ForeignKey("JobId")]
        public Job Job { get; set; }

        public int SkillId { get; set; }
        [ForeignKey("SkillId")]
        public Skill Skill { get; set; }
    }
}