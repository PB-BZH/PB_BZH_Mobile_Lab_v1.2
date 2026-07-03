using System.Text.Json;
using PB_BZH_Mobile_Lab.Core.Models;

namespace PB_BZH_Mobile_Lab.Core.Services;

public sealed class LicenseJsonService {
  private static readonly JsonSerializerOptions JsonOptions = new() {
    WriteIndented = true
  };

  public string GenerateLicenseJson(
    LicenseProfile profile) {

    ApplicationLicense license = new() {
      Product = profile.ProductId.Trim(),
      LicenseId = profile.LicenseId.Trim(),
      CustomerName = profile.CustomerName.Trim(),
      SiteName = profile.Site.Trim(),
      IssuedAt = DateOnly.FromDateTime(DateTime.Today),
      ValidUntil = profile.ValidUntil,
      MaintenanceUntil = profile.MaintenanceUntil,
      MachineHash = profile.MachineHash.Trim(),
      Features = [
        "Planning",
        "Pdf",
        "Mail",
        "Absences"
      ],
      Signature = string.Empty
    };

    return JsonSerializer.Serialize(
      license,
      JsonOptions);
  }
}