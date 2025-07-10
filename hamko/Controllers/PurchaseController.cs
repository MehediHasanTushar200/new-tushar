using hamko.Models;
using hamko.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hamko.Controllers
{
    public class PurchaseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PurchaseController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Index()
        {
            var purchase = _context.Purchases.ToList();
            return View(purchase);
        }

        public IActionResult Create()
        {
            var model = new Purchase
            {
                Date = DateTime.Now,
                InvoiceNo = GenerateInvoiceNumber(),
                RefNo = GenerateRefNumber(),
                StockIns = new List<StockIn> { new StockIn() }
            };
            ViewBag.Users = _context.User.ToList();
            ViewBag.Suppliers = _context.Suppliers.ToList();

            ViewBag.Products = _context.Products.ToList();

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
        public IActionResult Create(Purchase purchase, List<StockIn> StockIns)
        {
            _context.Purchases.Add(purchase);
            _context.SaveChanges();

            foreach (var stock in StockIns)
            {
                if (stock.ProductId == 0 || stock.Quantity <= 0 || stock.Price <= 0)
                    continue;

                stock.PurchaseId = purchase.Id;
                _context.StockIns.Add(stock);
            }



            TempData["success"] = "Purchase has been saved successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var purchase = _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .FirstOrDefault(p => p.Id == id);

            if (purchase == null)
                return NotFound();

            var stockIns = _context.StockIns
                .Include(s => s.Product)
                .Where(s => s.PurchaseId == id)
                .ToList();

            var viewModel = new PurchaseDetailsViewModel
            {
                Purchase = purchase,
                StockIns = stockIns
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var purchase = _context.Purchases.FirstOrDefault(p => p.Id == id);
            if (purchase != null)
            {
                var stockIns = _context.StockIns.Where(s => s.PurchaseId == id).ToList();
                _context.StockIns.RemoveRange(stockIns);

                _context.Purchases.Remove(purchase);
                _context.SaveChanges();

                TempData["success"] = "The record has been deleted.";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var purchase = await _context.Purchases
                .Include(p => p.StockIns)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null)
                return NotFound();

            ViewBag.Suppliers = await _context.Suppliers.ToListAsync();
            ViewBag.Users = await _context.User.ToListAsync();
            ViewBag.Products = await _context.Products.ToListAsync();

            return View(purchase);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Purchase purchase)
        {
            if (id != purchase.Id)
                return NotFound();

            var existingStockIns = _context.StockIns.Where(s => s.PurchaseId == id);
            _context.StockIns.RemoveRange(existingStockIns);
            //await _context.SaveChangesAsync();

            if (purchase.StockIns != null && purchase.StockIns.Any())
            {
                foreach (var stock in purchase.StockIns)
                {
                    stock.PurchaseId = purchase.Id;
                    _context.StockIns.Add(stock);
                }
            }

            var existingPurchase = await _context.Purchases.FindAsync(id);
            if (existingPurchase != null)
            {
                _context.Entry(existingPurchase).State = EntityState.Detached;
            }

            _context.Purchases.Update(purchase);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }



        // Removed: SearchBranches method
    }
}
