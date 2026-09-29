using BloodLine.Web.Data;
using BloodLine.Web.Models;
using BloodLine.Web.Services;
using BloodLine.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace BloodLine.Web.Controllers;

[Authorize(Roles = Roles.Manager)]
public class StaffController(AppDbContext db, BankScope scope, UserManager<AppUser> users, MailService mail, IConfiguration config) : Controller
{
    public async Task<IActionResult> Index()
    {
        var bank = await scope.GetBankId(User);
        var staff = await users.GetUsersInRoleAsync(Roles.Staff);
        return View(staff.Where(x => x.BloodBankId == bank).ToList());
    }
    [HttpGet] public IActionResult Create() => View(new StaffForm());
    [HttpPost]
    public async Task<IActionResult> Create(StaffForm f)
    {
        if (!ModelState.IsValid)
            return View(f);
        var bank = await scope.GetBankId(User);
        await using var tx = await db.Database.BeginTransactionAsync();
        var u = new AppUser { UserName = f.Email, Email = f.Email, FullName = f.FullName, Position = f.Position, BloodBankId = bank };
        var r = await users.CreateAsync(u);
        if (!r.Succeeded)
        {
            foreach (var e in r.Errors)
                ModelState.AddModelError("", e.Description);
            return View(f);
        }
        var role = await users.AddToRoleAsync(u, Roles.Staff);
        if (!role.Succeeded)
            throw new InvalidOperationException("Unable to assign staff role.");
        await tx.CommitAsync();
        var token = await users.GeneratePasswordResetTokenAsync(u);
        var link = config["PublicBaseUrl"]!.TrimEnd('/') + Url.Action("Reset", "Account", new
        {
            email = u.Email,
            token
        });
        try
        {
            await mail.Send(f.Email, "Welcome to BloodLine", "Set your password: " + link);
            TempData["Success"] = "Staff account created and invitation sent.";
        }
        catch (Exception) { TempData["Error"] = "Account created, but email failed. Fix mail settings, then use Forgot password."; }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var bank = await scope.GetBankId(User);
        var u = await users.FindByIdAsync(id);
        if (u == null || u.BloodBankId != bank || !await users.IsInRoleAsync(u, Roles.Staff))
            return NotFound();
        await users.DeleteAsync(u);
        TempData["Success"] = "Staff account removed.";
        return RedirectToAction(nameof(Index));
    }
}
