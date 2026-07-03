using System.Text.Json.Serialization;

namespace PB_BZH_Mobile_Lab.Core.Models;

public sealed class LicenseProfile {
  public string ProfileId { get; set; } = string.Empty;
  public string ProductId { get; set; } = string.Empty;
  public string LicenseId { get; set; } = string.Empty;
  public string CustomerName { get; set; } = string.Empty;
  public string Site { get; set; } = string.Empty;
  public string EmailContact { get; set; } = string.Empty;
  public string MachineHash { get; set; } = string.Empty;
  public DateOnly? ValidUntil { get; set; }
  public DateOnly? MaintenanceUntil { get; set; }
  public string ProfileName { get; set; } = string.Empty;

  [JsonIgnore]
  internal string SourceFilePath { get; set; } = string.Empty;

  public string DisplayName =>
    !string.IsNullOrWhiteSpace(ProfileName)
      ? ProfileName
      : BuildDisplayName();

  private string BuildDisplayName() {
    if (!string.IsNullOrWhiteSpace(CustomerName)
        && !string.IsNullOrWhiteSpace(ProductId)) {
      return $"{CustomerName} - {ProductId}";
    }

    if (!string.IsNullOrWhiteSpace(ProductId)) {
      return ProductId;
    }

    return "Nouveau profil";
  }
}
