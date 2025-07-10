using hamko.Models;
using hamko.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hamko.Controllers
{
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public SalesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Index()
        {
            var sales = _context.Sales.ToList();
            return View(sales);
        }


        public IActionResult Create()
        {
            var model = new Sales
            {
                Date = DateTime.Now,
                InvoiceNo = GenerateInvoiceNumber(),
                RefNo = GenerateRefNumber(),
                StockOuts = new List<StockOut> { new StockOut() }
            };

            var productIdsInStockIn = _context.StockIns
                .Select(si => si.ProductId)
                .Distinct()
                .ToList();

            var productsInStock = _context.Products
                .Where(p => productIdsInStockIn.Contains(p.Id))
                .ToList();

            var stockData = _context.StockIns
                .GroupBy(si => si.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    AvailableQty = g.Sum(si => si.Quantity)
                })
                .ToList();

            // Include Customers
            ViewBag.Customers = _context.Customers.ToList();

            // Include Users
            ViewBag.Users = _context.User.ToList();


            ViewBag.Products = productsInStock;
            ViewBag.StockData = stockData;

            return View(model);
        }


        private string GenerateInvoiceNumber()
        {
            return "Hamko-" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
        }

        private string GenerateRefNumber()
        {
            return "REF-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Sales sales, List<StockOut> StockOuts)
        {
            if (StockOuts == null || StockOuts.Count == 0)
            {
                TempData["error"] = "No product has been selected!";
                return RedirectToAction("Create");
            }

            var validStockOuts = StockOuts
                .Where(s => s.ProductId != 0 && s.Quantity > 0 && s.Price > 0)
                .ToList();

            var duplicateCheck = new HashSet<int>();

            foreach (var stock in validStockOuts)
            {
                if (duplicateCheck.Contains(stock.ProductId))
                {
                    TempData["error"] = "Multiple entries have been made for the same product!";
                    return RedirectToAction("Create");
                }
                duplicateCheck.Add(stock.ProductId);

                var stockIn = _context.StockIns
                    .Where(s => s.ProductId == stock.ProductId)
                    .OrderByDescending(s => s.Id)
                    .FirstOrDefault();

                if (stockIn == null || stockIn.Quantity < stock.Quantity)
                {
                    var product = _context.Products.Find(stock.ProductId);
                    TempData["error"] = $"Insufficient stock for product \"{product?.Name ?? "Unknown"}\".";
                    return RedirectToAction("Create");
                }

                stockIn.Quantity -= stock.Quantity;
                _context.Entry(stockIn).Property(s => s.Quantity).IsModified = true;
            }

            _context.Sales.Add(sales);
            _context.SaveChanges();

            TempData["success"] = "The sale has been completed successfully!";
            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            var sales = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .FirstOrDefault(s => s.Id == id);

            if (sales == null)
                return NotFound();

            var stockOuts = _context.StockOuts
                .Include(so => so.Product)
                .Where(so => so.SalesId == id)
                .ToList();

            var viewModel = new SalesDetailsViewModel
            {
                Sales = sales,
                StockOuts = stockOuts
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var sales = _context.Sales.FirstOrDefault(s => s.Id == id);
            if (sales == null)
            {
                TempData["error"] = "Sale not found!";
                return RedirectToAction("Index");
            }

            var stockOuts = _context.StockOuts.Where(s => s.SalesId == sales.Id).ToList();
            _context.StockOuts.RemoveRange(stockOuts);

            _context.Sales.Remove(sales);
            _context.SaveChanges();

            TempData["success"] = "Sale and related stock entries deleted successfully!";
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var sales = _context.Sales
                .Include(s => s.StockOuts)
                .FirstOrDefault(s => s.Id == id);

            if (sales == null) return NotFound();

            ViewBag.Customers = _context.Customers.ToList();
            ViewBag.Users = _context.User.ToList();
            ViewBag.Products = _context.Products.ToList();

            return View(sales);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Sales sales, List<StockOut> StockOuts)
        {
            if (id != sales.Id)
            {
                TempData["error"] = "Invalid Sales ID.";
                return RedirectToAction("Edit", new { id });
            }

            if (StockOuts == null || StockOuts.Count == 0)
            {
                TempData["error"] = "No product has been selected!";
                return RedirectToAction("Edit", new { id });
            }

            var validStockOuts = StockOuts
                .Where(s => s.ProductId != 0 && s.Quantity > 0 && s.Price > 0)
                .ToList();

            var duplicateCheck = new HashSet<int>();
            foreach (var stock in validStockOuts)
            {
                if (duplicateCheck.Contains(stock.ProductId))
                {
                    TempData["error"] = "Multiple entries have been made for the same product!";
                    return RedirectToAction("Edit", new { id });
                }
                duplicateCheck.Add(stock.ProductId);
            }

            var existingSales = _context.Sales.Include(s => s.StockOuts).FirstOrDefault(s => s.Id == id);
            if (existingSales == null)
            {
                TempData["error"] = "Sales record not found.";
                return RedirectToAction("Index");
            }

            foreach (var oldStock in existingSales.StockOuts)
            {
                var stockIn = _context.StockIns
                    .Where(si => si.ProductId == oldStock.ProductId)
                    .OrderByDescending(si => si.Id)
                    .FirstOrDefault();

                if (stockIn != null)
                {
                    stockIn.Quantity += oldStock.Quantity;
                    _context.Entry(stockIn).Property(si => si.Quantity).IsModified = true;
                }
            }

            foreach (var stock in validStockOuts)
            {
                var stockIn = _context.StockIns
                    .Where(si => si.ProductId == stock.ProductId)
                    .OrderByDescending(si => si.Id)
                    .FirstOrDefault();

                if (stockIn == null || stockIn.Quantity < stock.Quantity)
                {
                    var product = _context.Products.Find(stock.ProductId);
                    TempData["error"] = $"Insufficient stock for product \"{product?.Name ?? "Unknown"}\".";
                    return RedirectToAction("Edit", new { id });
                }
            }

            foreach (var stock in validStockOuts)
            {
                var stockIn = _context.StockIns
                    .Where(si => si.ProductId == stock.ProductId)
                    .OrderByDescending(si => si.Id)
                    .FirstOrDefault();

                stockIn.Quantity -= stock.Quantity;
                _context.Entry(stockIn).Property(si => si.Quantity).IsModified = true;
            }

            try
            {
                existingSales.Date = sales.Date;
                existingSales.CustomerId = sales.CustomerId;
                existingSales.InvoiceNo = sales.InvoiceNo;
                existingSales.RefNo = sales.RefNo;

                _context.StockOuts.RemoveRange(existingSales.StockOuts);

                foreach (var stock in validStockOuts)
                {
                    stock.SalesId = id;
                    _context.StockOuts.Add(stock);
                }

                _context.SaveChanges();
                TempData["success"] = "The sale has been updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Error updating sale: {ex.Message}";
                return RedirectToAction("Edit", new { id });
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public JsonResult SearchProducts(string term)
        {
            var productIdsInStockIn = _context.StockIns
                .Select(si => si.ProductId)
                .Distinct()
                .ToList();

            var products = _context.Products
                .Where(p => productIdsInStockIn.Contains(p.Id) && p.Name.Contains(term))
                .Select(p => new { label = p.Name, value = p.Id })
                .ToList();

            return Json(products);
        }

        [HttpGet]
        public JsonResult SearchCustomers(string term)
        {
            var customerIdsInSales = _context.Sales
                .Select(s => s.CustomerId)
                .Distinct()
                .ToList();

            var customers = _context.Customers
                .Where(c => customerIdsInSales.Contains(c.Id) && c.Name.Contains(term))
                .Select(c => new { label = c.Name, value = c.Id })
                .ToList();

            return Json(customers);
        }

    }
}
