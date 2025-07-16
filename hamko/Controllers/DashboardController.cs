using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hamko.Models;
using System.Linq;
using hamko.Service;

namespace hamko.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var totalPurchase = _context.StockIns
                .Select(x => (decimal)(x.Quantity * x.Price))
                .Sum();

            var totalSales = _context.StockOuts
                .Select(x => (decimal)(x.Quantity * x.Price))
                .Sum();

            var totalStockIn = _context.StockIns.Sum(x => x.Quantity);
            var totalStockOut = _context.StockOuts.Sum(x => x.Quantity);

            ViewBag.TotalPurchase = totalPurchase;
            ViewBag.TotalSales = totalSales;
            ViewBag.TotalStockIn = totalStockIn;
            ViewBag.TotalStockOut = totalStockOut;

            return View();
        }

    }
}
