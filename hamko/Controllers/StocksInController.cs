using hamko.Models;
using hamko.Service;
using hamko.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hamko.Controllers
{
    public class StocksInController : Controller
    {
        private readonly ApplicationDbContext context;

        public StocksInController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var groupedStockIn = context.StockIns
                .Include(s => s.Product)
                .ToList()
                .GroupBy(s => new { s.ProductId, s.Product.Name })
                .Select(g => new StockInGroupedViewModel
                {
                    ProductId = g.Key.ProductId,
                    Name = g.Key.Name,
                    Quantity = g.Sum(x => x.Quantity),
                    Price = g.Average(x => x.Price)
                }).ToList();

            var model = new StockInSearchViewModel
            {
                Results = groupedStockIn
            };

            return View(model);
        }
    }
}
