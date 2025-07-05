
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using hamko.Service;

namespace hamko.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Report/Index
        public IActionResult Index()
        {
            // Pass group list to View for dropdown
            ViewBag.Groups = _context.Groups.Where(g => g.Status).ToList();
            return View();
        }
        [HttpPost]
        public IActionResult Generate(int? groupId, DateTime? startDate, DateTime? endDate)
        {
            ViewBag.Groups = _context.Groups.Where(g => g.Status).ToList();

            DateTime start = startDate ?? DateTime.Today;
            DateTime end = endDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Today.AddDays(1).AddTicks(-1);

            var itemsQuery = _context.Items.AsQueryable();

            if (groupId.HasValue)
            {
                itemsQuery = itemsQuery.Where(i => i.GroupId == groupId.Value);
            }

            itemsQuery = itemsQuery.Where(i => i.CreatedDate >= start && i.CreatedDate <= end);

            var filteredItems = itemsQuery
                .Include(i => i.Group)
                .ToList();

            // Preserve selected values
            ViewBag.SelectedGroupId = groupId;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View("Index", filteredItems);
        }



    }
}
