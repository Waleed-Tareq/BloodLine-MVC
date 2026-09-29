using BloodLine.Web.Models;
using BloodLine.Web.Services;
using BloodLine.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace BloodLine.Web.Controllers;

[Authorize]
public class AccountController(UserManager<AppUser> users, SignInManager<AppUser> signIn, MailService mail, IConfiguration config, ILogger<AccountController> logger) : Controller
{
    [AllowAnonymous, HttpGet] public IActionResult Login(string? returnUrl = null) => View(new LoginForm { ReturnUrl = returnUrl });
    [AllowAnonymous, HttpPost, EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Login(LoginForm f)
    {
        if (!ModelState.IsValid)
            return View(f);
        var result = await signIn.PasswordSignInAsync(f.Email, f.Password, f.RememberMe, true);
        if (result.Succeeded)
            return LocalRedirect(Url.IsLocalUrl(f.ReturnUrl) ? f.ReturnUrl! : "/");
        ModelState.AddModelError("", result.IsLockedOut ? "Too many attempts. Try again in 15 minutes." : "Invalid email or password.");
        return View(f);
    }
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
    [AllowAnonymous] public IActionResult Denied() => View();
    [AllowAnonymous, HttpGet] public IActionResult Forgot() => View(new EmailForm());
    [AllowAnonymous, HttpPost, EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Forgot(EmailForm f)
    {
        if (!ModelState.IsValid)
            return View(f);
        var u = await users.FindByEmailAsync(f.Email);
        if (u != null)
        {
            var token = await users.GeneratePasswordResetTokenAsync(u);
            var path = Url.Action(nameof(Reset), "Account", new
            {
                email = f.Email,
                token
            })!;
            try
            {
                await mail.Send(f.Email, "BloodLine password reset", config["PublicBaseUrl"]!.TrimEnd('/') + path);
            }
            catch (Exception) { logger.LogWarning("Password reset email delivery failed; check mail configuration."); }
        }
        TempData["Success"] = "If this account exists, a reset link has been sent. In local development, check App_Data/mail.";
        return RedirectToAction(nameof(Login));
    }
    [AllowAnonymous, HttpGet] public IActionResult Reset(string email, string token) => View(new ResetForm { Email = email, Token = token });
    [AllowAnonymous, HttpPost, EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Reset(ResetForm f)
    {
        if (!ModelState.IsValid)
            return View(f);
        var u = await users.FindByEmailAsync(f.Email);
        if (u == null)
        {
            ModelState.AddModelError("", "Invalid reset request.");
            return View(f);
        }
        var result = await users.ResetPasswordAsync(u, f.Token, f.Password);
        if (result.Succeeded)
        {
            TempData["Success"] = "Password set. You can sign in now.";
            return RedirectToAction(nameof(Login));
        }
        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);
        return View(f);
    }
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var u = await users.GetUserAsync(User);
        if (u == null)
            return Challenge();
        return View(new ProfileForm { FullName = u.FullName, PhoneNumber = u.PhoneNumber, Gender = u.Gender, DateOfBirth = u.DateOfBirth });
    }
    [HttpPost]
    public async Task<IActionResult> Profile(ProfileForm f)
    {
        if (f.DateOfBirth > DateOnly.FromDateTime(DateTime.Today))
            ModelState.AddModelError(nameof(f.DateOfBirth), "Date of birth cannot be in the future.");
        if (!ModelState.IsValid)
            return View(f);
        var u = await users.GetUserAsync(User);
        if (u == null)
            return Challenge();
        u.FullName = f.FullName;
        u.PhoneNumber = f.PhoneNumber;
        u.Gender = f.Gender;
        u.DateOfBirth = f.DateOfBirth;
        var result = await users.UpdateAsync(u);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return View(f);
        }
        await signIn.RefreshSignInAsync(u);
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }
    [HttpGet] public IActionResult Password() => View(new PasswordForm());
    [HttpPost]
    public async Task<IActionResult> Password(PasswordForm f)
    {
        if (!ModelState.IsValid)
            return View(f);
        var u = await users.GetUserAsync(User);
        if (u == null)
            return Challenge();
        var r = await users.ChangePasswordAsync(u, f.CurrentPassword, f.NewPassword);
        if (r.Succeeded)
        {
            await signIn.RefreshSignInAsync(u);
            TempData["Success"] = "Password changed.";
            return RedirectToAction(nameof(Profile));
        }
        foreach (var e in r.Errors)
            ModelState.AddModelError("", e.Description);
        return View(f);
    }
}
