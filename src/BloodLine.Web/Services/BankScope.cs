using BloodLine.Web.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
namespace BloodLine.Web.Services;

public class BankScope(UserManager<AppUser> users)
{
    public async Task<int> GetBankId(ClaimsPrincipal principal)
    {
        var user = await users.GetUserAsync(principal);
        return user?.BloodBankId ?? throw new InvalidOperationException("This account is not assigned to a blood bank.");
    }
}
