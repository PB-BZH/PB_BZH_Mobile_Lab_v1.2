namespace PB_BZH_Mobile_Lab.Core.Models;

public sealed class ApplicationLicense {
  public string Product { get; set; } = string.Empty;
  public string LicenseId { get; set; } = string.Empty;
  public string CustomerName { get; set; } = string.Empty;
  public string SiteName { get; set; } = string.Empty;
  public DateOnly IssuedAt { get; set; }
  public DateOnly? ValidUntil { get; set; }
  public DateOnly? MaintenanceUntil { get; set; }
  public string MachineHash { get; set; } = string.Empty;
  public List<string> Features { get; set; } = [];
  public string Signature { get; set; } = string.Empty;
}