using hamko.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hamko.Controllers
{
    public class StocksOutController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;

        public StocksOutController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            this.context = context;
            this.environment = environment;
        }

        public IActionResult Index()
        {
            var stockOuts = context.StockOuts
                .Include(s => s.Product)
                .Include(s => s.Sales)
                .ToList();

            return View(stockOuts);
        }

    }
}
