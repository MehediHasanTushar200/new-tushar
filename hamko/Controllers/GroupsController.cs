using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using hamko.Models;

using hamko.Service;

public class GroupsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly string _imageFolder = "wwwroot/images/groups";

    public GroupsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Groups
    public async Task<IActionResult> Index()
    {
        var groups = await _context.Groups
                            .Include(g => g.Parent) // Parent গ্রুপ লোড করার জন্য
                            .ToListAsync();

        return View(groups);
    }

    // GET: Groups/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var group = await _context.Groups
            .Include(g => g.Parent)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (group == null) return NotFound();

        return View(group);
    }

    // GET: Groups/Create
    public IActionResult Create()
    {
        ViewData["ParentId"] = new SelectList(_context.Groups, "Id", "Name");
        return View();
    }

    // POST: Groups/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Group group, IFormFile ImageFile)
    {
        //if (ModelState.IsValid)
        //{
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
                group.Image = "/images/groups/" + fileName;
            }

            _context.Add(group);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        //}

        //ViewData["ParentId"] = new SelectList(_context.Groups, "Id", "Name", group.ParentId);
        //return View(group);
    }

    // GET: Groups/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var group = await _context.Groups.FindAsync(id);
        if (group == null) return NotFound();

        ViewData["ParentId"] = new SelectList(_context.Groups.Where(g => g.Id != id), "Id", "Name", group.ParentId);
        return View(group);
    }

    // POST: Groups/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Group group, IFormFile ImageFile)
    {
        if (id != group.Id) return NotFound();

        //if (ModelState.IsValid)
        //{
            try
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
                    group.Image = "/images/groups/" + fileName;
                }
                else
                {
                    // Preserve old image path if no new image uploaded
                    var oldGroup = await _context.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
                    group.Image = oldGroup?.Image;
                }

                _context.Update(group);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GroupExists(group.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        //}
        //ViewData["ParentId"] = new SelectList(_context.Groups.Where(g => g.Id != id), "Id", "Name", group.ParentId);
        //return View(group);
    }

    // GET: Groups/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var group = await _context.Groups
            .Include(g => g.Children)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null)
        {
            return NotFound();
        }

        if (group.Children != null && group.Children.Any())
        {
            TempData["Error"] = "Cannot delete group with child groups. Remove or reassign child groups first.";
            return RedirectToAction(nameof(Index));
        }

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Group deleted successfully.";
        return RedirectToAction(nameof(Index));
    }




    private bool GroupExists(int id)
    {
        return _context.Groups.Any(e => e.Id == id);
    }
}
