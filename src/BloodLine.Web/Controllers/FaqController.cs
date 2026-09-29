using BloodLine.Web.Data;
using BloodLine.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace BloodLine.Web.Controllers;

[Authorize]
public class FaqController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Faqs.OrderBy(x => x.Id).ToListAsync());
    [Authorize(Roles = Roles.Admin), HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return View(new Faq());
        var f = await db.Faqs.FindAsync(id);
        return f == null ? NotFound() : View(f);
    }
    [Authorize(Roles = Roles.Admin), HttpPost]
    public async Task<IActionResult> Edit(int? id, [Bind("Question,Answer")] Faq f)
    {
        if (!ModelState.IsValid)
            return View(f);
        if (id == null)
        {
            f.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            db.Faqs.Add(f);
        }
        else
        {
            var saved = await db.Faqs.FindAsync(id);
            if (saved == null)
                return NotFound();
            saved.Question = f.Question;
            saved.Answer = f.Answer;
        }
        await db.SaveChangesAsync();
        TempData["Success"] = "FAQ saved.";
        return RedirectToAction(nameof(Index));
    }
    [Authorize(Roles = Roles.Admin), HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var f = await db.Faqs.FindAsync(id);
        if (f == null)
            return NotFound();
        db.Faqs.Remove(f);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
