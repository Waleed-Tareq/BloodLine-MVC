using BloodLine.Web.Data;
using BloodLine.Web.Models;
using BloodLine.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace BloodLine.Web.Controllers;

[Authorize(Roles = Roles.Staff)]
public class EventsController(AppDbContext db, BankScope scope) : Controller
{
    public async Task<IActionResult> Index()
    {
        var bank = await scope.GetBankId(User);
        return View(await db.Events.Where(x => x.BloodBankId == bank).OrderByDescending(x => x.StartsAt).ToListAsync());
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return View(new BankEvent());
        var bank = await scope.GetBankId(User);
        var f = await db.Events.SingleOrDefaultAsync(x => x.Id == id && x.BloodBankId == bank);
        return f == null ? NotFound() : View(f);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(int? id, [Bind("Title,Description,StartsAt,Location")] BankEvent f)
    {
        var bank = await scope.GetBankId(User);
        if (f.StartsAt < DateTime.Now)
            ModelState.AddModelError(nameof(f.StartsAt), "Choose a future date and time.");
        if (!ModelState.IsValid)
            return View(f);
        if (id == null)
        {
            f.BloodBankId = bank;
            db.Events.Add(f);
        }
        else
        {
            var saved = await db.Events.SingleOrDefaultAsync(x => x.Id == id && x.BloodBankId == bank);
            if (saved == null)
                return NotFound();
            saved.Title = f.Title;
            saved.Description = f.Description;
            saved.StartsAt = f.StartsAt;
            saved.Location = f.Location;
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "Saved successfully.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var bank = await scope.GetBankId(User);
        var f = await db.Events.SingleOrDefaultAsync(x => x.Id == id && x.BloodBankId == bank);
        if (f == null)
            return NotFound();
        db.Events.Remove(f);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
