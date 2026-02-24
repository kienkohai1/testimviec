using Microsoft.EntityFrameworkCore;
using testimviec.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Candidate> Candidate { get; set; }
    public DbSet<Job> Job { get; set; }

    // Thêm các DbSet mới
    public DbSet<Skill> Skills { get; set; }
    public DbSet<CandidateSkill> CandidateSkills { get; set; }
    public DbSet<JobSkill> JobSkills { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình khóa chính cho bảng trung gian CandidateSkill
        modelBuilder.Entity<CandidateSkill>()
            .HasKey(cs => new { cs.CandidateId, cs.SkillId });

        // Cấu hình khóa chính cho bảng trung gian JobSkill
        modelBuilder.Entity<JobSkill>()
            .HasKey(js => new { js.JobId, js.SkillId });
    }
}