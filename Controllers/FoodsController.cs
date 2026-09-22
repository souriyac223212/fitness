using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FitWiseBD.Models;
using FitWiseBD.Data;

public class FoodsController : Controller
{
    private readonly ApplicationDbContext _context;

    public FoodsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Foods
    public async Task<IActionResult> Index()
    {
        return View(await _context.Foods.ToListAsync());
    }

    // GET: Foods/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var food = await _context.Foods
            .FirstOrDefaultAsync(m => m.FoodId == id);

        if (food == null)
        {
            return NotFound();
        }

        return View(food);
    }

    // GET: Foods/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Foods/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("FoodId,Name,Calories,Protein,Carbs,Fat,Cost")] Food food)
    {
        if (ModelState.IsValid)
        {
            _context.Add(food);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        return View(food);
    }

    // GET: Foods/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var food = await _context.Foods.FindAsync(id);

        if (food == null)
        {
            return NotFound();
        }

        return View(food);
    }

    // POST: Foods/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int? id,
        [Bind("FoodId,Name,Calories,Protein,Carbs,Fat,Cost")] Food food)
    {
        if (id != food.FoodId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(food);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FoodExists(food.FoodId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        return View(food);
    }

    // GET: Foods/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var food = await _context.Foods
            .FirstOrDefaultAsync(m => m.FoodId == id);

        if (food == null)
        {
            return NotFound();
        }

        return View(food);
    }

    // POST: Foods/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var food = await _context.Foods.FindAsync(id);

        if (food != null)
        {
            _context.Foods.Remove(food);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private bool FoodExists(int id)
    {
        return _context.Foods.Any(e => e.FoodId == id);
    }
}