using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace BloodLine.Web.Models;

public static class Roles
{
    public const string Admin = "Admin", Manager = "Manager", Staff = "Staff";
}
public static class BloodGroups
{
    public static readonly string[] All = ["A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"];
}
public sealed class BloodGroupAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is string s && BloodGroups.All.Contains(s);
    public override string FormatErrorMessage(string name) => "Select a valid blood group.";
}
public class AppUser : IdentityUser
{
    [Required, StringLength(100)] public string FullName { get; set; } = "";
    public int? BloodBankId
    {
        get; set;
    }
    public BloodBank? BloodBank
    {
        get; set;
    }
    [StringLength(100)] public string Position { get; set; } = "";
    [StringLength(30)] public string Gender { get; set; } = "";
    public DateOnly? DateOfBirth
    {
        get; set;
    }
}
public class BloodBank
{
    public int Id
    {
        get; set;
    }
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [Range(-90, 90)]
    public double Latitude
    {
        get; set;
    }
    [Range(-180, 180)]
    public double Longitude
    {
        get; set;
    }
    [Required, Phone, StringLength(50)] public string Phone { get; set; } = "";
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    public TimeOnly OpensAt { get; set; } = new(9, 0);
    public TimeOnly ClosesAt { get; set; } = new(17, 0);
}
public class RegistrationRequest
{
    public int Id
    {
        get; set;
    }
    [Required, StringLength(100)] public string ManagerName { get; set; } = "";
    [Required, EmailAddress, StringLength(200)] public string ManagerEmail { get; set; } = "";
    [Required, StringLength(100)] public string ManagerPosition { get; set; } = "";
    [Required, StringLength(200)] public string OrganizationName { get; set; } = "";
    [Range(-90, 90)]
    public double Latitude
    {
        get; set;
    }
    [Range(-180, 180)]
    public double Longitude
    {
        get; set;
    }
    [Required, Phone, StringLength(50)] public string ContactPhone { get; set; } = "";
    public TimeOnly OpensAt { get; set; } = new(9, 0);
    public TimeOnly ClosesAt { get; set; } = new(17, 0);
    public string Status { get; set; } = "Pending";
    public string? ReviewNote
    {
        get; set;
    }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
public class Donor
{
    public int Id
    {
        get; set;
    }
    public int BloodBankId
    {
        get; set;
    }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    [Phone, StringLength(50)]
    public string? Phone
    {
        get; set;
    }
    [Required, BloodGroup] public string BloodGroup { get; set; } = "O+";
    public DateOnly? DateOfBirth
    {
        get; set;
    }
    [StringLength(30)] public string Gender { get; set; } = "";
    [Range(1, 500)] public double Weight { get; set; } = 60;
    [StringLength(100)]
    public string? IdNumber
    {
        get; set;
    }
    [StringLength(1000)]
    public string? Conditions
    {
        get; set;
    }
    public bool IsVolunteer
    {
        get; set;
    }
    public int RankingPoints
    {
        get; set;
    }
}
public enum AppointmentStatus
{
    Pending, Open, Complete, Canceled
}
public class Appointment
{
    public int Id
    {
        get; set;
    }
    public int BloodBankId
    {
        get; set;
    }
    public int DonorId
    {
        get; set;
    }
    public Donor Donor { get; set; } = null!;
    public DateTime ScheduledAt
    {
        get; set;
    }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    [Required, StringLength(50)] public string DonationType { get; set; } = "Whole blood";
    public Guid Version { get; set; } = Guid.NewGuid();
}
public class Donation
{
    public int Id
    {
        get; set;
    }
    public int BloodBankId
    {
        get; set;
    }
    public int DonorId
    {
        get; set;
    }
    public Donor Donor { get; set; } = null!;
    public int AppointmentId
    {
        get; set;
    }
    public DateTime DonatedAt
    {
        get; set;
    }
    public string BloodGroup { get; set; } = "";
    public string DonationType { get; set; } = "";
    public double Units
    {
        get; set;
    }
    public double Pulse
    {
        get; set;
    }
    public double Temperature
    {
        get; set;
    }
    public string BloodPressure { get; set; } = "";
}
public class InventoryBatch
{
    public int Id
    {
        get; set;
    }
    public int BloodBankId
    {
        get; set;
    }
    public string BloodGroup { get; set; } = "";
    public double Units
    {
        get; set;
    }
    public DateOnly ExpiresOn
    {
        get; set;
    }
    public Guid Version { get; set; } = Guid.NewGuid();
}
public class InventoryIssue
{
    public int Id
    {
        get; set;
    }
    public int BloodBankId
    {
        get; set;
    }
    public string BloodGroup { get; set; } = "";
    public double Units
    {
        get; set;
    }
    public string Recipient { get; set; } = "";
    public string IssuedBy { get; set; } = "";
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}
public class BankEvent
{
    public int Id
    {
        get; set;
    }
    public int BloodBankId
    {
        get; set;
    }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Required, StringLength(1000)] public string Description { get; set; } = "";
    public DateTime StartsAt { get; set; } = DateTime.Today.AddDays(1).AddHours(9);
    [Required, StringLength(200)] public string Location { get; set; } = "";
}
public class BloodNeed
{
    public int Id
    {
        get; set;
    }
    public int BloodBankId
    {
        get; set;
    }
    [Required, BloodGroup] public string BloodGroup { get; set; } = "O+";
    [Range(0.01, 10000)] public double Units { get; set; } = 1;
    [Required, StringLength(200)] public string Location { get; set; } = "";
    [Required, StringLength(200)] public string Hospital { get; set; } = "";
    public DateTime ExpiresAt { get; set; } = DateTime.Today.AddDays(1);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class Faq
{
    public int Id
    {
        get; set;
    }
    [Required, StringLength(500)] public string Question { get; set; } = "";
    [Required, StringLength(1000)] public string Answer { get; set; } = "";
    public string CreatedBy { get; set; } = "";
}
