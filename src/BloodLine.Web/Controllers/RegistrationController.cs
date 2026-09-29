using BloodLine.Web.Data;
using BloodLine.Web.Models;
using BloodLine.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
namespace BloodLine.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class RegistrationController(AppDbContext db, UserManager<AppUser> users, MailService mail, IConfiguration config) : Controller
{
    [AllowAnonymous, HttpGet] public IActionResult Create() => View(new RegistrationRequest());
    [AllowAnonymous, HttpPost, EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Create([Bind("ManagerName,ManagerEmail,ManagerPosition,OrganizationName,Latitude,Longitude,ContactPhone,OpensAt,ClosesAt")] RegistrationRequest f)
    {
        if (f.OpensAt >= f.ClosesAt)
            ModelState.AddModelError("", "Closing time must be after opening time.");
        if (!ModelState.IsValid)
            return View(f);
        if (await db.RegistrationRequests.AnyAsync(x => x.ManagerEmail == f.ManagerEmail && x.Status == "Pending"))
        {
            ModelState.AddModelError("", "A pending request already exists for this email.");
            return View(f);
        }
        db.RegistrationRequests.Add(f);
        await db.SaveChangesAsync();
        TempData["Success"] = "Registration request submitted for review.";
        return RedirectToAction("Login", "Account");
    }
    public async Task<IActionResult> Index() => View(await db.RegistrationRequests.OrderByDescending(x => x.SubmittedAt).ToListAsync());
    [HttpPost]
    public async Task<IActionResult> Review(int id, bool approve, string? note)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var r = await db.RegistrationRequests.SingleOrDefaultAsync(x => x.Id == id);
        if (r == null)
            return NotFound();
        if (r.Status != "Pending")
            return BadRequest("This request has already been reviewed.");
        if (note?.Length > 1000)
            return BadRequest("Review note is too long.");
        r.ReviewNote = note;
        AppUser? u = null;
        if (approve)
        {
            if (await users.FindByEmailAsync(r.ManagerEmail) != null)
            {
                TempData["Error"] = "That email already has an account.";
                return RedirectToAction(nameof(Index));
            }
            var bank = new BloodBank { Name = r.OrganizationName, Latitude = r.Latitude, Longitude = r.Longitude, Phone = r.ContactPhone, Email = r.ManagerEmail, OpensAt = r.OpensAt, ClosesAt = r.ClosesAt };
            db.BloodBanks.Add(bank);
            await db.SaveChangesAsync();
            u = new AppUser { UserName = r.ManagerEmail, Email = r.ManagerEmail, FullName = r.ManagerName, Position = r.ManagerPosition, BloodBankId = bank.Id };
            var created = await users.CreateAsync(u);
            if (!created.Succeeded)
            {
                TempData["Error"] = string.Join("; ", created.Errors.Select(x => x.Description));
                return RedirectToAction(nameof(Index));
            }
            var role = await users.AddToRoleAsync(u, Roles.Manager);
            if (!role.Succeeded)
                throw new InvalidOperationException("Unable to assign manager role.");
            r.Status = "Approved";
        }
        else
            r.Status = "Rejected";
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        var body = $"Your BloodLine registration request was {r.Status.ToLowerInvariant()}.\n{note}";
        if (u != null)
        {
            var token = await users.GeneratePasswordResetTokenAsync(u);
            body += "\nSet your password: " + config["PublicBaseUrl"]!.TrimEnd('/') + Url.Action("Reset", "Account", new
            {
                email = u.Email,
                token
            });
        }
        try
        {
            await mail.Send(r.ManagerEmail, "BloodLine registration update", body);
            TempData["Success"] = "Review saved and notification sent.";
        }
        catch (Exception) { TempData["Error"] = "Review saved, but email delivery failed. Fix mail settings; the manager can then use Forgot password."; }
        return RedirectToAction(nameof(Index));
    }
}
