using Microsoft.AspNetCore.Mvc;
using testimviec.Models;
using Mscc.GenerativeAI;
using System.Text.Json;
using UglyToad.PdfPig;
namespace testimviec.Controllers
{
    public class CVController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _apiKey = "AIzaSyAV-pBonQcQypN3hiPWXijYsoq63-qBdU4"; // Thay bằng Key của bạn

        public CVController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var candidates = _context.Candidates.OrderByDescending(c => c.AnalyzedAt).ToList();
            return View(candidates);
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

            // 1. Trích xuất Text từ PDF
            string rawText = "";
            using (var document = PdfDocument.Open(cvFile.OpenReadStream()))
            {
                foreach (var page in document.GetPages())
                {
                    rawText += page.Text;
                }
            }

            // 2. Gọi Gemini AI để phân tích
            var googleAI = new GoogleAI(_apiKey);
            var model = googleAI.GenerativeModel("gemini-1.5-flash-001");

            string prompt = $@"Phân tích CV sau và trả về DUY NHẤT định dạng JSON (không kèm lời dẫn). 
            Cấu trúc: {{ ""FullName"": """", ""Email"": """", ""Phone"": """", ""Skills"": [], ""ExperienceYears"": 0, ""Summary"": """" }}
            Nội dung CV: {rawText}";

            var response = await model.GenerateContent(prompt);
            string jsonString = (response.Text ?? string.Empty).Replace("```json", "").Replace("```", "").Trim();

            // 3. Chuyển JSON thành Object và Lưu Database
            var data = JsonSerializer.Deserialize<Candidate>(jsonString);

            if (data != null)
            {
                // Gán thêm các thông tin không có trong JSON
                data.RawJsonResult = jsonString;
                data.AnalyzedAt = DateTime.Now;

                // Giả sử bạn muốn lưu kỹ năng thành chuỗi cách nhau bởi dấu phẩy
                // (Cần chỉnh lại logic tùy theo cách bạn xử lý mảng Skills trong Model)

                _context.Candidates.Add(data);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}