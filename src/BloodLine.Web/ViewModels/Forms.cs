using System.ComponentModel.DataAnnotations;
using BloodLine.Web.Models;
namespace BloodLine.Web.ViewModels;

public class LoginForm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool RememberMe
    {
        get; set;
    }
    public string? ReturnUrl
    {
        get; set;
    }
}
public class EmailForm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
}
public class ResetForm : EmailForm
{
    [Required] public string Token { get; set; } = "";
    [Required, MinLength(12), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Compare(nameof(Password)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}
public class PasswordForm
{
    [Required, DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
    [Required, MinLength(12), DataType(DataType.Password)] public string NewPassword { get; set; } = "";
    [Compare(nameof(NewPassword)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}
public class ProfileForm
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Phone]
    public string? PhoneNumber
    {
        get; set;
    }
    [StringLength(30)] public string Gender { get; set; } = "";
    public DateOnly? DateOfBirth
    {
        get; set;
    }
}
public class StaffForm
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, StringLength(100)] public string Position { get; set; } = "";
}
public class AppointmentForm
{
    [Range(1, int.MaxValue)]
    public int DonorId
    {
        get; set;
    }
    public DateTime ScheduledAt { get; set; } = DateTime.Today.AddHours(10);
    [Required, StringLength(50)] public string DonationType { get; set; } = "Whole blood";
}
public class CompleteDonationForm
{
    public int AppointmentId
    {
        get; set;
    }
    [Required, BloodGroup] public string BloodGroup { get; set; } = "O+";
    [Range(0.01, 100)] public double Units { get; set; } = 1;
    [Range(1, 300)]
    public double Pulse
    {
        get; set;
    }
    [Range(1, 50)]
    public double Temperature
    {
        get; set;
    }
    [Required, RegularExpression(@"^\d{2,3}/\d{2,3}$", ErrorMessage = "Use systolic/diastolic format, for example 120/80.")] public string BloodPressure { get; set; } = "";
    public DateOnly ExpiresOn { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(42));
}
public class IssueForm
{
    [Required, BloodGroup] public string BloodGroup { get; set; } = "O+";
    [Range(0.01, 10000)] public double Units { get; set; } = 1;
    [Required, StringLength(200)] public string Recipient { get; set; } = "";
}
public class DashboardViewModel
{
    public string BankName { get; set; } = "BloodLine";
    public int Donors
    {
        get; set;
    }
    public int Appointments
    {
        get; set;
    }
    public double AvailableUnits
    {
        get; set;
    }
    public int PendingRequests
    {
        get; set;
    }
    public int Staff
    {
        get; set;
    }
    public int Events
    {
        get; set;
    }
}
