using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PB_BZH_Mobile_Lab.Core.Models;

namespace PB_BZH_Mobile_Lab.Core.Services;

public sealed class LicenseFileService {
  private static readonly JsonSerializerOptions SigningJsonOptions = new() {
    WriteIndented = false,
    PropertyNamingPolicy = null
  };

  private static readonly JsonSerializerOptions OutputJsonOptions = new() {
    WriteIndented = true
  };

  public async Task<string> GenerateLicenseFileAsync(
    LicenseProfile profile) {

    Validate(profile);

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

    using RSA rsa =
      LoadPrivateKey();

    string unsignedJson =
      JsonSerializer.Serialize(
        license,
        SigningJsonOptions);

    byte[] signature =
      rsa.SignData(
        Encoding.UTF8.GetBytes(unsignedJson),
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1);

    license.Signature =
      Convert.ToBase64String(signature);

    string json =
      JsonSerializer.Serialize(
        license,
        OutputJsonOptions);

    string filePath =
      Path.Combine(
        FileSystem.CacheDirectory,
        BuildFileName(profile));

    await File.WriteAllTextAsync(
      filePath,
      json,
      new UTF8Encoding(false));

    return filePath;
  }

  private static void Validate(
    LicenseProfile profile) {

    if (string.IsNullOrWhiteSpace(profile.ProductId)) {
      throw new InvalidOperationException("Le produit doit être renseigné.");
    }

    if (string.IsNullOrWhiteSpace(profile.LicenseId)) {
      throw new InvalidOperationException("L'identifiant de licence doit être renseigné.");
    }

    if (string.IsNullOrWhiteSpace(profile.CustomerName)) {
      throw new InvalidOperationException("Le client doit être renseigné.");
    }

    if (string.IsNullOrWhiteSpace(profile.MachineHash)) {
      throw new InvalidOperationException("L'identifiant machine doit être renseigné.");
    }
  }

  private static RSA LoadPrivateKey() {
    string privateKeyPath =
      GetPrivateKeyPath();

    if (!File.Exists(privateKeyPath)) {
      throw new InvalidOperationException(
        "La clé privée est introuvable.\n\n" +
        "Importez private_key.pem dans l'application avant de générer une licence.");
    }

    RSA rsa = RSA.Create();

    rsa.ImportFromPem(
      File.ReadAllText(privateKeyPath));

    return rsa;
  }

  public static string GetPrivateKeyPath() {
    return Path.Combine(
      FileSystem.AppDataDirectory,
      "private_key.pem");
  }

  private static string BuildFileName(
    LicenseProfile profile) {

    string product =
      SanitizeFileName(profile.ProductId);

    string customer =
      SanitizeFileName(profile.CustomerName);

    return $"license-{product}-{customer}-{DateTime.Now:yyyyMMdd-HHmmss}.lic";
  }

  private static string SanitizeFileName(
    string value) {

    string sanitized =
      value.Trim();

    foreach (char invalidChar in Path.GetInvalidFileNameChars()) {
      sanitized = sanitized.Replace(invalidChar,'-');
    }

    return string.IsNullOrWhiteSpace(sanitized)
      ? "client"
      : sanitized;
  }
}