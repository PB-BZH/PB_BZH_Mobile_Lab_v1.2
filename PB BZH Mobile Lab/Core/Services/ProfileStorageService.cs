using System.Text.Json;
using PB_BZH_Mobile_Lab.Core.Models;

namespace PB_BZH_Mobile_Lab.Core.Services;

public sealed class ProfileStorageService {
  private static readonly JsonSerializerOptions JsonOptions = new() {
    WriteIndented = true
  };

  internal static string ProfilesDirectory {
    get {
      string directory =
        Path.Combine(
          FileSystem.AppDataDirectory,
          "Profiles");

      Directory.CreateDirectory(directory);

      return directory;
    }
  }

  public async Task<List<LicenseProfile>> LoadProfilesAsync() {
    List<LicenseProfile> profiles = [];

    foreach (string filePath in Directory.EnumerateFiles(
      ProfilesDirectory,
      "*.json")) {

      LicenseProfile? profile =
        await LoadProfileFromFileAsync(filePath);

      if (profile is not null) {
        profiles.Add(profile);
      }
    }

    return profiles
      .OrderBy(p => p.DisplayName)
      .ToList();
  }

  public async Task SaveProfileAsync(
    LicenseProfile profile) {

    if (string.IsNullOrWhiteSpace(profile.ProfileName)) {
      profile.ProfileName = profile.DisplayName;
    }

    string filePath =
      GetProfileFilePath(profile);

    string json =
      JsonSerializer.Serialize(
        profile,
        JsonOptions);

    await File.WriteAllTextAsync(
      filePath,
      json);
  }

  internal void DeleteProfile(
    LicenseProfile profile) {

    string filePath = GetProfileFilePath(profile);

    if (File.Exists(filePath)) {
      File.Delete(filePath);
    }
  }

  private static async Task<LicenseProfile?> LoadProfileFromFileAsync(
    string filePath) {

    try {
      string json =
        await File.ReadAllTextAsync(filePath);

      if (string.IsNullOrWhiteSpace(json)) {
        return null;
      }

      return JsonSerializer.Deserialize<LicenseProfile>(
        json,
        JsonOptions);
    }
    catch {
      return null;
    }
  }

  private static string GetProfileFilePath(
    LicenseProfile profile) {

    string profileName =
      !string.IsNullOrWhiteSpace(profile.ProfileName)
        ? profile.ProfileName
        : profile.DisplayName;

    return Path.Combine(
      ProfilesDirectory,
      SanitizeFileName(profileName) + ".json");
  }

  private static string SanitizeFileName(
    string value) {

    string result =
      string.IsNullOrWhiteSpace(value)
        ? "profil"
        : value.Trim();

    foreach (char invalidChar in Path.GetInvalidFileNameChars()) {
      result = result.Replace(invalidChar,'-');
    }

    return result;
  }
}