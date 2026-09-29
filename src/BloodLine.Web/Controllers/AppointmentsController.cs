using BloodLine.Web.Data;
using BloodLine.Web.Models;
using BloodLine.Web.Services;
using BloodLine.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace BloodLine.Web.Controllers;

[Authorize(Roles = Roles.Staff)]
public class AppointmentsController(AppDbContext db, BankScope scope, DonationService service) : Controller
{
    public async Task<IActionResult> Index(DateTime? date, AppointmentStatus? status)
    {
        var bank = await scope.GetBankId(User);
        var day = (date ?? DateTime.Today).Date;
        var next = day.AddDays(1);
        var q = db.Appointments.Include(x => x.Donor).Where(x => x.BloodBankId == bank && x.ScheduledAt >= day && x.ScheduledAt < next);
        if (status != null)
            q = q.Where(x => x.Status == status);
        ViewBag.Day = day;
        ViewBag.Status = status;
        return View(await q.OrderBy(x => x.ScheduledAt).ToListAsync());
    }
    async Task DonorOptions()
    {
        var bank = await scope.GetBankId(User);
        ViewBag.Donors = new SelectList(await db.Donors.Where(x => x.BloodBankId == bank).OrderBy(x => x.Name).ToListAsync(), "Id", "Name");
    }
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await DonorOptions();
        return View(new AppointmentForm());
    }
    [HttpPost]
    public async Task<IActionResult> Create(AppointmentForm f)
    {
        var bank = await scope.GetBankId(User);
        if (!await db.Donors.AnyAsync(x => x.Id == f.DonorId && x.BloodBankId == bank))
            ModelState.AddModelError(nameof(f.DonorId), "Choose a donor from your bank.");
        if (f.ScheduledAt.Date < DateTime.Today)
            ModelState.AddModelError(nameof(f.ScheduledAt), "Choose today or a future date.");
        if (await db.Appointments.AnyAsync(x => x.BloodBankId == bank && x.DonorId == f.DonorId && (x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Open)))
            ModelState.AddModelError("", "This donor already has an active appointment.");
        if (!ModelState.IsValid)
        {
            await DonorOptions();
            return View(f);
        }
        db.Appointments.Add(new Appointment { BloodBankId = bank, DonorId = f.DonorId, ScheduledAt = f.ScheduledAt, DonationType = f.DonationType });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new
        {
            date = f.ScheduledAt.ToString("yyyy-MM-dd")
        });
    }
    [HttpPost]
    public async Task<IActionResult> Transition(int id, string operation)
    {
        var bank = await scope.GetBankId(User);
        var a = await db.Appointments.SingleOrDefaultAsync(x => x.Id == id && x.BloodBankId == bank);
        if (a == null)
            return NotFound();
        if (operation == "open" && a.Status == AppointmentStatus.Pending)
            a.Status = AppointmentStatus.Open;
        else if (operation == "cancel" && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Open))
            a.Status = AppointmentStatus.Canceled;
        else
            return BadRequest("Invalid appointment transition.");
        a.Version = Guid.NewGuid();
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException) { TempData["Error"] = "Appointment changed. Refresh and try again."; }
        return RedirectToAction(nameof(Index), new
        {
            date = a.ScheduledAt.ToString("yyyy-MM-dd")
        });
    }
    [HttpGet]
    public async Task<IActionResult> Complete(int id)
    {
        var bank = await scope.GetBankId(User);
        var a = await db.Appointments.Include(x => x.Donor).SingleOrDefaultAsync(x => x.Id == id && x.BloodBankId == bank && x.Status == AppointmentStatus.Open);
        if (a == null)
            return NotFound();
        ViewBag.DonorName = a.Donor.Name;
        return View(new CompleteDonationForm { AppointmentId = id, BloodGroup = a.Donor.BloodGroup });
    }
    [HttpPost]
    public async Task<IActionResult> Complete(CompleteDonationForm f)
    {
        if (!ModelState.IsValid)
            return View(f);
        try
        {
            await service.Complete(await scope.GetBankId(User), f);
            TempData["Success"] = "Donation recorded and inventory updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException e) { ModelState.AddModelError("", e.Message); }
        catch (DbUpdateException) { ModelState.AddModelError("", "The record changed or was already completed. Refresh and try again."); }
        return View(f);
    }
}
