using Microsoft.AspNetCore.Mvc;
using testimviec.Models;
using System.Text.Json;
using UglyToad.PdfPig;
using System.Net.Http.Headers;
using System.Text;

namespace testimviec.Controllers
{
    // DTO để hứng dữ liệu từ AI
    public class CandidateDto
    {
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public required List<string> Skills { get; set; }
        public int ExperienceYears { get; set; }
        public required string Strengths { get; set; }
        public required string Weaknesses { get; set; }
        public required string Summary { get; set; }
    }

    public class CVController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public CVController(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var candidates = _context.Candidate.ToList();
            return View(candidates);
        }

        public async Task<IActionResult> Details(int id)
        {
            var candidate = await _context.Candidate.FindAsync(id);
            if (candidate == null) return NotFound();

            var candidateSkills = (candidate.Skills ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToList();

            var suggestedJobsQuery = _context.Job.AsQueryable();

            if (candidateSkills.Any())
            {
                suggestedJobsQuery = suggestedJobsQuery
                    .Where(job =>
                        job.Skills != null &&
                        job.Skills
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(s => s.ToLowerInvariant())
                            .Any(js => candidateSkills.Contains(js))
                        && candidate.ExperienceYears >= job.MinExperienceYears
                    );
            }

            var suggestedJobs = suggestedJobsQuery
                .OrderByDescending(j => j.MinExperienceYears)
                .Take(10)
                .ToList();

            ViewBag.SuggestedJobs = suggestedJobs;
            return View(candidate);
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile cvFile)
        {
            if (cvFile == null || cvFile.Length == 0) return Content("File không hợp lệ");

            try
            {
                // 1. Lưu file gốc
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + cvFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await cvFile.CopyToAsync(fileStream);
                }

                // 2. Trích xuất Text từ PDF
                string rawText = "";
                using (var document = PdfDocument.Open(cvFile.OpenReadStream()))
                {
                    foreach (var page in document.GetPages())
                    {
                        rawText += page.Text + " ";
                    }
                }

                // --- LÀM SẠCH VĂN BẢN ĐỂ TRÁNH LỖI JSON ---
                string cleanText = rawText.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
                while (cleanText.Contains("  ")) cleanText = cleanText.Replace("  ", " ");
                cleanText = cleanText.Trim();
                if (cleanText.Length > 3000) cleanText = cleanText.Substring(0, 3000);

                // 3. Gọi Groq API
                var apiKey = _configuration["Groq:ApiKey"];
                var modelName = _configuration["Groq:Model"];
                var apiUrl = "https://api.groq.com/openai/v1/chat/completions";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                const string jsonSchema = @"{
  ""FullName"": """",
  ""Email"": """",
  ""Phone"": """",
  ""Skills"": [],
  ""ExperienceYears"": 0,
  ""Strengths"": """",
  ""Weaknesses"": """",
  ""Summary"": """"
}";

                var requestBody = new
                {
                    model = modelName,
                    messages = new[]
                    {
                        new {
                            role = "system",
                            content = "Bạn là máy trích xuất dữ liệu từ CV. Nhiệm vụ: trả về ĐÚNG MỘT object JSON hợp lệ, không bọc trong markdown (không dùng ```json hay ```), không thêm lời giải thích. Chỉ in ra nội dung JSON thuần. Nếu thiếu trường thì để chuỗi rỗng \"\" hoặc 0. Skills là mảng chuỗi. Strengths là mô tả ngắn gọn điểm mạnh của ứng viên, Weaknesses là mô tả ngắn gọn điểm yếu / điểm cần cải thiện."
                        },
                        new {
                            role = "user",
                            content = $"Trích xuất thông tin CV sau vào đúng format JSON này (chỉ trả về JSON, không code block):\n{jsonSchema}\n\nNội dung CV:\n{cleanText}"
                        }
                    },
                    response_format = new { type = "json_object" },
                    temperature = 0.1,
                    max_tokens = 1024
                };

                var jsonPayload = JsonSerializer.Serialize(requestBody);
                var httpResponse = await client.PostAsync(apiUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                var responseContent = await httpResponse.Content.ReadAsStringAsync();

                // Retry nếu lỗi JSON
                if (!httpResponse.IsSuccessStatusCode && responseContent.Contains("json_validate_failed") && cleanText.Length > 1500)
                {
                    cleanText = cleanText.Substring(0, 1500);
                    requestBody = new
                    {
                        model = modelName,
                        messages = new[]
                        {
                            new { role = "system", content = "Bạn là máy trích xuất dữ liệu từ CV. Trả về ĐÚNG MỘT object JSON hợp lệ, không markdown, không giải thích. Chỉ JSON thuần. Thiếu thì để \"\" hoặc 0. Skills là mảng chuỗi. Strengths là mô tả điểm mạnh, Weaknesses là mô tả điểm yếu / điểm cần cải thiện." },
                            new { role = "user", content = $"Trích xuất CV thành JSON (chỉ JSON, không code block):\n{jsonSchema}\n\nNội dung:\n{cleanText}" }
                        },
                        response_format = new { type = "json_object" },
                        temperature = 0.1,
                        max_tokens = 1024
                    };
                    jsonPayload = JsonSerializer.Serialize(requestBody);
                    httpResponse = await client.PostAsync(apiUrl, new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                    responseContent = await httpResponse.Content.ReadAsStringAsync();
                }

                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Content($"Lỗi API Groq: {responseContent}");
                }

                // 4. Parse dữ liệu
                using var jsonDoc = JsonDocument.Parse(responseContent);
                string? resultJson = jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrEmpty(resultJson))
                {
                    return Content("Lỗi: API Groq không trả về nội dung.");
                }

                // 5. Deserialize
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var dto = JsonSerializer.Deserialize<CandidateDto>(resultJson, options);

                if (dto == null)
                {
                    return Content("Lỗi: Không phân tích được CV từ phản hồi của AI.");
                }

                ViewBag.FilePath = "/uploads/" + uniqueFileName;
                ViewBag.RawJsonResult = resultJson;

                return View("AnalyzeResult", dto);
            }
            catch (Exception ex)
            {
                return Content($"Lỗi hệ thống: {ex.Message}");
            }
        }

        [HttpPost]
        // Đã thêm tham số skillsString vào đây để nhận dữ liệu từ form
        public async Task<IActionResult> Confirm(CandidateDto dto, string filePath, string rawJsonResult, string skillsString)
        {
            if (dto == null)
            {
                return Content("Lỗi: Dữ liệu xác nhận không hợp lệ.");
            }

            var candidate = new Candidate
            {
                FullName = dto.FullName ?? "Không rõ",
                Email = dto.Email ?? string.Empty,
                Phone = dto.Phone ?? string.Empty,
                
                // Logic quan trọng: Lấy từ skillsString (chuỗi người dùng đã sửa trên form)
                Skills = !string.IsNullOrEmpty(skillsString) 
                            ? skillsString 
                            : (dto.Skills != null ? string.Join(", ", dto.Skills) : string.Empty),

                ExperienceYears = dto.ExperienceYears,
                Strengths = dto.Strengths ?? string.Empty,
                Weaknesses = dto.Weaknesses ?? string.Empty,
                Summary = dto.Summary ?? string.Empty,
                FilePath = filePath ?? string.Empty,
                AnalyzedAt = DateTime.Now,
                RawJsonResult = rawJsonResult ?? string.Empty
            };

            _context.Candidate.Add(candidate);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var candidate = await _context.Candidate.FindAsync(id);
            if (candidate != null)
            {
                _context.Candidate.Remove(candidate);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}