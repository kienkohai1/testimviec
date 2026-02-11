using Microsoft.EntityFrameworkCore;

namespace testimviec.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Candidate> Candidate { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Bạn có thể cấu hình thêm các ràng buộc dữ liệu tại đây nếu cần
        }
    }
}