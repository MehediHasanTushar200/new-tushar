using hamko.Models;
using hamko.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace hamko.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _imageFolder = "wwwroot/images/products";

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.Include(p => p.Group).ToListAsync();
            var items = await _context.Items.Include(p => p.Group).ToListAsync(); // or just .ToListAsync() if no nav prop
                                                                                  // If you want to pass both to the view, create a ViewModel or pass via ViewBag
            return View(products);
        }


        // GET: Products/Create
        public IActionResult Create()
        {
            ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Name");
            ViewData["ItemId"] = new SelectList(_context.Items, "Id", "Name");
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            //if (ModelState.IsValid)
            //{
                if (product.ImageFile != null)
                {
                    var fileName = Path.GetFileName(product.ImageFile.FileName);
                    var filePath = Path.Combine("wwwroot/images/products/", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await product.ImageFile.CopyToAsync(stream);
                    }

                    product.Image = "/images/products/" + fileName;
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            //}

            //ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Name", product.GroupId);
            //ViewData["ItemId"] = new SelectList(_context.Items, "Id", "Name", product.ItemId);
            //return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Prepare dropdown lists
            ViewBag.GroupId = new SelectList(_context.Groups, "Id", "Name", product.GroupId);
            ViewBag.ItemId = new SelectList(_context.Items, "Id", "Name", product.ItemId);

            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
            //{
                try
                {
                    // Handle image upload if new image is selected
                    if (product.ImageFile != null && product.ImageFile.Length > 0)
                    {
                        var imageFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products/");

                        if (!Directory.Exists(imageFolder))
                            Directory.CreateDirectory(imageFolder);

                        var fileName = Path.GetFileName(product.ImageFile.FileName);
                        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                        var filePath = Path.Combine(imageFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await product.ImageFile.CopyToAsync(stream);
                        }

                        product.Image = "/images/products/" + uniqueFileName;
                    }
                    else
                    {
                        // Keep the existing image if no new image uploaded
                        var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                        product.Image = existingProduct?.Image;
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product updated successfully.";
            }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            //}

            //// If ModelState invalid, reload dropdowns
            //ViewBag.GroupId = new SelectList(_context.Groups, "Id", "Name", product.GroupId);
            //ViewBag.ItemId = new SelectList(_context.Items, "Id", "Name", product.ItemId);
            //return View(product);
        }



        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Group)
                .Include(p => p.Item)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }


        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Group)
                .Include(p => p.Item)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // এখানে চাইলে চাইল্ড রিলেশন চেক করতে পারেন, যদি থাকে
            // উদাহরণ: if (product.SomeChildren != null && product.SomeChildren.Any()) { ... }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

    }
}
