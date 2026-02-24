using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using testimviec.Models;

namespace testimviec.Controllers
{
    public class JobController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- 1. DANH SÁCH VIỆC LÀM ---
        public async Task<IActionResult> Index()
        {
            // Include JobSkills và Skill để hiển thị tên kỹ năng ra danh sách
            var jobs = await _context.Job
                .Include(j => j.JobSkills)
                .ThenInclude(js => js.Skill)
                .ToListAsync();
            return View(jobs);
        }

        // --- 2. CHI TIẾT VIỆC LÀM ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Job
                .Include(j => j.JobSkills)
                .ThenInclude(js => js.Skill)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (job == null) return NotFound();

            return View(job);
        }


            // --- 1. Sửa Action CREATE (GET) ---
            public IActionResult Create()
            {
                // Lấy danh sách Skill từ DB để hiển thị trong dropdown
                ViewData["Skills"] = new SelectList(_context.Skills, "SkillId", "Name");
                return View();
            }

            // --- 2. Sửa Action CREATE (POST) ---
            [HttpPost]
            [ValidateAntiForgeryToken]
            // Thay đổi tham số: nhận int[] selectedSkills thay vì string skillsInput
            public async Task<IActionResult> Create(Job job, int[] selectedSkills)
            {
                if (ModelState.IsValid)
                {
                    _context.Add(job);
                    await _context.SaveChangesAsync();

                    // Gọi hàm xử lý lưu Skill theo ID
                    await UpdateJobSkillsFromIds(job.Id, selectedSkills);

                    return RedirectToAction(nameof(Index));
                }
                // Nếu lỗi, load lại danh sách skill
                ViewData["Skills"] = new SelectList(_context.Skills, "SkillId", "Name", selectedSkills);
                return View(job);
            }

            // --- 3. Sửa Action EDIT (GET) ---
            public async Task<IActionResult> Edit(int? id)
            {
                if (id == null) return NotFound();

                var job = await _context.Job
                    .Include(j => j.JobSkills)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (job == null) return NotFound();

                // Lấy danh sách ID các skill hiện có của Job này để "tô chọn" sẵn
                var currentSkillIds = job.JobSkills.Select(js => js.SkillId).ToList();

                // Dùng MultiSelectList để tự động chọn các skill đã có
                ViewData["Skills"] = new MultiSelectList(_context.Skills, "SkillId", "Name", currentSkillIds);

                return View(job);
            }

            // --- 4. Sửa Action EDIT (POST) ---
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Edit(int id, Job job, int[] selectedSkills)
            {
                if (id != job.Id) return NotFound();

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(job);
                        await _context.SaveChangesAsync();

                        // Update Skills theo danh sách ID
                        await UpdateJobSkillsFromIds(id, selectedSkills);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!JobExists(job.Id)) return NotFound();
                        else throw;
                    }
                    return RedirectToAction(nameof(Index));
                }
                // Nếu lỗi, load lại danh sách
                ViewData["Skills"] = new MultiSelectList(_context.Skills, "SkillId", "Name", selectedSkills);
                return View(job);
            }

            // --- HÀM PHỤ TRỢ MỚI: Xử lý lưu Skill theo ID ---
            private async Task UpdateJobSkillsFromIds(int jobId, int[] selectedSkillIds)
            {
                // 1. Xóa các skill cũ
                var oldSkills = _context.JobSkills.Where(js => js.JobId == jobId);
                _context.JobSkills.RemoveRange(oldSkills);
                await _context.SaveChangesAsync();

                if (selectedSkillIds == null || selectedSkillIds.Length == 0) return;

                // 2. Thêm skill mới từ danh sách ID đã chọn
                // Loại bỏ trùng lặp ID (nếu có)
                foreach (var skillId in selectedSkillIds.Distinct())
                {
                    _context.JobSkills.Add(new JobSkill
                    {
                        JobId = jobId,
                        SkillId = skillId
                    });
                }
                await _context.SaveChangesAsync();
            }

            // --- 5. XÓA VIỆC LÀM ---
            public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Job
                .FirstOrDefaultAsync(m => m.Id == id);

            if (job == null) return NotFound();

            // Xóa job (Cascade delete sẽ tự xóa trong bảng JobSkill nếu database cấu hình đúng,
            // hoặc EF Core sẽ tự xử lý nếu load tracking)
            _context.Job.Remove(job);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // --- HÀM PHỤ TRỢ: Xử lý lưu Skill ---
        private async Task UpdateJobSkills(int jobId, string skillsInput)
        {
            // 1. Xóa các skill cũ của Job này trong bảng trung gian
            var oldSkills = _context.JobSkills.Where(js => js.JobId == jobId);
            _context.JobSkills.RemoveRange(oldSkills);
            await _context.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(skillsInput)) return;

            // 2. Tách chuỗi nhập vào
            var skillNames = skillsInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                        .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var name in skillNames)
            {
                // Tìm skill trong DB (không phân biệt hoa thường)
                var dbSkill = await _context.Skills
                    .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());

                // Nếu chưa có thì tạo mới Skill
                if (dbSkill == null)
                {
                    dbSkill = new Skill { Name = name };
                    _context.Skills.Add(dbSkill);
                    await _context.SaveChangesAsync();
                }

                // Tạo liên kết vào bảng JobSkill
                _context.JobSkills.Add(new JobSkill
                {
                    JobId = jobId,
                    SkillId = dbSkill.SkillId
                });
            }
            await _context.SaveChangesAsync();
        }

        private bool JobExists(int id)
        {
            return _context.Job.Any(e => e.Id == id);
        }
        // --- THÊM: Action tạo kỹ năng nhanh bằng Ajax ---
        [HttpPost]
        public async Task<IActionResult> CreateSkillQuick(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                return Json(new { success = false, message = "Tên kỹ năng không được để trống" });
            }

            // 1. Kiểm tra trùng lặp (không phân biệt hoa thường)
            var existing = await _context.Skills
                .FirstOrDefaultAsync(s => s.Name.ToLower() == skillName.Trim().ToLower());

            if (existing != null)
            {
                return Json(new { success = false, message = "Kỹ năng này đã tồn tại!" });
            }

            // 2. Tạo mới
            var newSkill = new Skill { Name = skillName.Trim() };
            _context.Skills.Add(newSkill);
            await _context.SaveChangesAsync();

            // 3. Trả về JSON để JavaScript xử lý
            return Json(new { success = true, id = newSkill.SkillId, name = newSkill.Name });
        }
    }
}