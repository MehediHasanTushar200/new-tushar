using hamko.Models;
using hamko.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class ItemsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly string _imageFolder = "wwwroot/images/items";

    public ItemsController(ApplicationDbContext context)
    {
        _context = context;
    }


    public async Task<IActionResult> Index(int page = 1)
    {
        int pageSize = 50;

        var query = _context.Items
                    .Include(i => i.Group)
                    .OrderBy(i => i.Id);

        int totalItems = await query.CountAsync();

        var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

        var model = new ItemListViewModel
        {
            Items = items,
            PageNumber = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };

        return View(model);
    }



    // Create page (GET)
    public IActionResult Create()
    {
        ViewBag.GroupId = new SelectList(_context.Groups, "Id", "Name");
        var item = new Item();  // নতুন Item instance তৈরি করুন
        return View(item);      // View-এ পাস করুন
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Item item, IFormFile ImageFile)
    {
        if (ImageFile != null && ImageFile.Length > 0)
        {
            if (!Directory.Exists(_imageFolder))
                Directory.CreateDirectory(_imageFolder);

            var fileName = Path.GetFileName(ImageFile.FileName);
            var filePath = Path.Combine(_imageFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await ImageFile.CopyToAsync(stream);
            }
            item.Image = "/images/items/" + fileName;
        }

        //if (ModelState.IsValid)
        //{
        _context.Add(item);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
        //}
        //ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Name", item.GroupId);
        //return View(item);
    }


    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var item = await _context.Items.FindAsync(id);
        if (item == null) return NotFound();

        ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Name", item.GroupId);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Item item, IFormFile ImageFile)
    {
        if (id != item.Id) return NotFound();

        if (ImageFile != null && ImageFile.Length > 0)
        {
            if (!Directory.Exists(_imageFolder))
                Directory.CreateDirectory(_imageFolder);

            var fileName = Path.GetFileName(ImageFile.FileName);
            var filePath = Path.Combine(_imageFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await ImageFile.CopyToAsync(stream);
            }
            item.Image = "/images/items/" + fileName;
        }
        else
        {
            var oldItem = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
            item.Image = oldItem?.Image;
        }

        //if (ModelState.IsValid)
        //{
            _context.Update(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        //}
        //ViewData["GroupId"] = new SelectList(_context.Groups, "Id", "Name", item.GroupId);
        //return View(item);
    }
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var item = await _context.Items
                                 .Include(i => i.Group)
                                 .FirstOrDefaultAsync(m => m.Id == id);

        if (item == null)
            return NotFound();

        return View(item);
    }


    // GET: Item/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.Items
            .Include(i => i.Group)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
        {
            return NotFound();
        }

        // যদি item এর কোনো child relation থাকত, তাহলে এখানে চেক করতে হতো।
        // Example: if (item.Children != null && item.Children.Any()) { ... }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Item deleted successfully.";
        return RedirectToAction(nameof(Index));
    }



}