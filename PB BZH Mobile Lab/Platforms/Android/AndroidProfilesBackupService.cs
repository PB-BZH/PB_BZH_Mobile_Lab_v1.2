using System.Globalization;
using System.IO.Compression;
using System.Text;
using PB_BZH_Mobile_Lab.Core.Services;

namespace PB_BZH_Mobile_Lab.Platforms.Android;

public sealed class AndroidProfilesBackupService: IProfilesBackupService {
  private const string BackupFolderName = "PB_BZH_Mobile_Lab";
  private const string BackupFilePrefix = "PB_BZH_Mobile_Lab_Profiles_";
  private const string ProfilesEntryFolder = "profiles";

  public async Task<string> ExporterAsync() {
    string fileName =
        BackupFilePrefix +
        DateTime.Now.ToString("yyyy-MM-dd_HHmm",CultureInfo.InvariantCulture) +
        ".zip";

    string tempZipPath =
        Path.Combine(FileSystem.CacheDirectory,fileName);

    if (File.Exists(tempZipPath)) {
      File.Delete(tempZipPath);
    }

    CreateBackupZip(tempZipPath);

    return await CopyZipToDocumentsAsync(tempZipPath,fileName);
  }

  public async Task ImporterAsync() {
    string tempZipPath =
        Path.Combine(
            FileSystem.CacheDirectory,
            "restore_profiles_" + Guid.NewGuid().ToString("N") + ".zip"
        );

    try {
      if ((int)global::Android.OS.Build.VERSION.SdkInt >= 29) {
        await CopyLatestBackupFromDocumentsWithMediaStoreAsync(tempZipPath);
      }
      else {
        await CopyLatestBackupFromDocumentsLegacyAsync(tempZipPath);
      }
    }
    catch {
      await CopyBackupSelectedByUserAsync(tempZipPath);
    }

    RestoreBackupZip(tempZipPath);
  }

  private static void CreateBackupZip(string zipPath) {
    string profilesDirectory =
        ProfileStorageService.ProfilesDirectory;

    string[] profileFiles =
        Directory.GetFiles(profilesDirectory,"*.json");

    if (profileFiles.Length == 0) {
      throw new InvalidOperationException(
          "Aucun profil à sauvegarder."
      );
    }

    using ZipArchive archive =
        ZipFile.Open(zipPath,ZipArchiveMode.Create);

    foreach (string profileFile in profileFiles) {
      string entryName =
          ProfilesEntryFolder +
          "/" +
          Path.GetFileName(profileFile);

      archive.CreateEntryFromFile(
          profileFile,
          entryName,
          CompressionLevel.Optimal
      );
    }

    ZipArchiveEntry metadataEntry =
        archive.CreateEntry("metadata.json",CompressionLevel.Optimal);

    using Stream metadataStream = metadataEntry.Open();
    using var writer =
        new StreamWriter(metadataStream,new UTF8Encoding(false));

    writer.WriteLine("{");
    writer.WriteLine("  \"Application\": \"PB_BZH_Mobile_Lab\",");
    writer.WriteLine("  \"Type\": \"ProfilesBackup\",");
    writer.WriteLine("  \"DateSauvegarde\": \"" +
                     DateTime.Now.ToString("O",CultureInfo.InvariantCulture) +
                     "\",");
    writer.WriteLine("  \"NombreProfils\": " +
                     profileFiles.Length.ToString(CultureInfo.InvariantCulture));
    writer.WriteLine("}");
  }

  private static async Task<string> CopyZipToDocumentsAsync(
      string sourceZipPath,
      string fileName) {

    if ((int)global::Android.OS.Build.VERSION.SdkInt >= 29) {
      return await CopyZipToDocumentsWithMediaStoreAsync(
          sourceZipPath,
          fileName
      );
    }

    return await CopyZipToDocumentsLegacyAsync(
        sourceZipPath,
        fileName
    );
  }

  private static async Task<string> CopyZipToDocumentsWithMediaStoreAsync(
      string sourceZipPath,
      string fileName) {

    global::Android.Content.ContentResolver resolver =
        Microsoft.Maui.ApplicationModel.Platform.AppContext.ContentResolver
        ?? throw new InvalidOperationException(
            "ContentResolver Android indisponible."
        );

    string relativePath =
        global::Android.OS.Environment.DirectoryDocuments +
        "/" +
        BackupFolderName;

    using var values =
        new global::Android.Content.ContentValues();

    values.Put(
        global::Android.Provider.MediaStore.IMediaColumns.DisplayName,
        fileName
    );

    values.Put(
        global::Android.Provider.MediaStore.IMediaColumns.MimeType,
        "application/zip"
    );

    values.Put(
        global::Android.Provider.MediaStore.IMediaColumns.RelativePath,
        relativePath
    );

    values.Put(
        global::Android.Provider.MediaStore.IMediaColumns.IsPending,
        1
    );

    global::Android.Net.Uri collection =
        global::Android.Provider.MediaStore.Files.GetContentUri(
            global::Android.Provider.MediaStore.VolumeExternalPrimary
        );

    global::Android.Net.Uri? uri =
        resolver.Insert(collection,values);

    if (uri is null) {
      throw new InvalidOperationException(
          "Impossible de créer le fichier de sauvegarde dans Documents."
      );
    }

    try {
      await using Stream input =
          File.OpenRead(sourceZipPath);

      await using Stream? output =
          resolver.OpenOutputStream(uri);

      if (output is null) {
        throw new InvalidOperationException(
            "Impossible d'ouvrir le fichier de sauvegarde Android."
        );
      }

      await input.CopyToAsync(output);

      values.Clear();

      values.Put(
          global::Android.Provider.MediaStore.IMediaColumns.IsPending,
          0
      );

      resolver.Update(uri,values,null,null);
    }
    catch {
      resolver.Delete(uri,null,null);
      throw;
    }

    return "Documents/" + BackupFolderName + "/" + fileName;
  }

  private static async Task<string> CopyZipToDocumentsLegacyAsync(
      string sourceZipPath,
      string fileName) {

    Java.IO.File? documentsDirectory =
        global::Android.OS.Environment.GetExternalStoragePublicDirectory(
            global::Android.OS.Environment.DirectoryDocuments
        );

    if (documentsDirectory is null) {
      throw new InvalidOperationException(
          "Le dossier Documents Android est indisponible."
      );
    }

    string backupDirectory =
        Path.Combine(
            documentsDirectory.AbsolutePath,
            BackupFolderName
        );

    Directory.CreateDirectory(backupDirectory);

    string destinationPath =
        Path.Combine(backupDirectory,fileName);

    await using FileStream source =
        File.OpenRead(sourceZipPath);

    await using FileStream destination =
        File.Create(destinationPath);

    await source.CopyToAsync(destination);

    return destinationPath;
  }

  private static async Task CopyLatestBackupFromDocumentsWithMediaStoreAsync(
      string destinationZipPath) {

    global::Android.Content.ContentResolver resolver =
        Microsoft.Maui.ApplicationModel.Platform.AppContext.ContentResolver
        ?? throw new InvalidOperationException(
            "ContentResolver Android indisponible."
        );

    global::Android.Net.Uri collection =
        global::Android.Provider.MediaStore.Files.GetContentUri(
            global::Android.Provider.MediaStore.VolumeExternalPrimary
        );

    string relativePathWithSlash =
        global::Android.OS.Environment.DirectoryDocuments +
        "/" +
        BackupFolderName +
        "/";

    string relativePathWithoutSlash =
        global::Android.OS.Environment.DirectoryDocuments +
        "/" +
        BackupFolderName;

    string[] projection = [
        "_id",
        global::Android.Provider.MediaStore.IMediaColumns.DisplayName,
        global::Android.Provider.MediaStore.IMediaColumns.DateModified
    ];

    string selection =
        "(" +
        global::Android.Provider.MediaStore.IMediaColumns.RelativePath +
        " = ? OR " +
        global::Android.Provider.MediaStore.IMediaColumns.RelativePath +
        " = ?) AND " +
        global::Android.Provider.MediaStore.IMediaColumns.DisplayName +
        " LIKE ?";

    string[] selectionArgs = [
        relativePathWithSlash,
        relativePathWithoutSlash,
        BackupFilePrefix + "%.zip"
    ];

    string sortOrder =
        global::Android.Provider.MediaStore.IMediaColumns.DateModified +
        " DESC";

    global::Android.Database.ICursor? cursor =
        resolver.Query(
            collection,
            projection,
            selection,
            selectionArgs,
            sortOrder
        );

    try {
      if (cursor is null || !cursor.MoveToFirst()) {
        throw new InvalidOperationException(
            "Aucune sauvegarde trouvée dans Documents/" +
            BackupFolderName +
            "."
        );
      }

      int idColumn =
          cursor.GetColumnIndexOrThrow("_id");

      long id =
          cursor.GetLong(idColumn);

      global::Android.Net.Uri backupUri =
          global::Android.Content.ContentUris.WithAppendedId(
              collection,
              id
          );

      await using Stream? input =
          resolver.OpenInputStream(backupUri);

      if (input is null) {
        throw new InvalidOperationException(
            "Impossible d'ouvrir la sauvegarde trouvée."
        );
      }

      await using FileStream output =
          File.Create(destinationZipPath);

      await input.CopyToAsync(output);
    }
    finally {
      cursor?.Close();
    }
  }

  private static async Task CopyLatestBackupFromDocumentsLegacyAsync(
      string destinationZipPath) {

    Java.IO.File? documentsDirectory =
        global::Android.OS.Environment.GetExternalStoragePublicDirectory(
            global::Android.OS.Environment.DirectoryDocuments
        );

    if (documentsDirectory is null) {
      throw new InvalidOperationException(
          "Le dossier Documents Android est indisponible."
      );
    }

    string backupDirectory =
        Path.Combine(
            documentsDirectory.AbsolutePath,
            BackupFolderName
        );

    if (!Directory.Exists(backupDirectory)) {
      throw new InvalidOperationException(
          "Aucune sauvegarde trouvée dans " + backupDirectory + "."
      );
    }

    string? latestBackup =
        Directory
            .GetFiles(
                backupDirectory,
                BackupFilePrefix + "*.zip"
            )
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

    if (latestBackup is null) {
      throw new InvalidOperationException(
          "Aucune sauvegarde PB BZH Mobile Lab trouvée dans " +
          backupDirectory +
          "."
      );
    }

    await using FileStream input =
        File.OpenRead(latestBackup);

    await using FileStream output =
        File.Create(destinationZipPath);

    await input.CopyToAsync(output);
  }

  private static async Task CopyBackupSelectedByUserAsync(
      string destinationZipPath) {

    FileResult? selectedFile =
        await FilePicker.Default.PickAsync(
            new PickOptions {
              PickerTitle = "Sélectionner une sauvegarde PB BZH Mobile Lab"
            }
        );

    if (selectedFile is null) {
      throw new InvalidOperationException(
          "Aucune sauvegarde sélectionnée."
      );
    }

    await using Stream source =
        await selectedFile.OpenReadAsync();

    await using FileStream destination =
        File.Create(destinationZipPath);

    await source.CopyToAsync(destination);
  }

  private static void RestoreBackupZip(string zipPath) {
    string restoreDirectory =
        Path.Combine(
            FileSystem.CacheDirectory,
            "restore_profiles_" + Guid.NewGuid().ToString("N")
        );

    Directory.CreateDirectory(restoreDirectory);

    using ZipArchive archive =
        ZipFile.OpenRead(zipPath);

    List<ZipArchiveEntry> profileEntries =
        archive
            .Entries
            .Where(e =>
                e.FullName.StartsWith(
                    ProfilesEntryFolder + "/",
                    StringComparison.OrdinalIgnoreCase
                ) &&
                e.FullName.EndsWith(
                    ".json",
                    StringComparison.OrdinalIgnoreCase
                ) &&
                !string.IsNullOrWhiteSpace(e.Name))
            .ToList();

    if (profileEntries.Count == 0) {
      throw new InvalidOperationException(
          "La sauvegarde sélectionnée ne contient aucun profil."
      );
    }

    BackupCurrentProfilesBeforeImport();

    string profilesDirectory =
        ProfileStorageService.ProfilesDirectory;

    foreach (string existingFile in Directory.GetFiles(profilesDirectory,"*.json")) {
      File.Delete(existingFile);
    }

    foreach (ZipArchiveEntry entry in profileEntries) {
      string fileName =
          Path.GetFileName(entry.Name);

      if (string.IsNullOrWhiteSpace(fileName)) {
        continue;
      }

      string destinationPath =
          Path.Combine(profilesDirectory,fileName);

      entry.ExtractToFile(destinationPath,overwrite: true);
    }
  }

  private static void BackupCurrentProfilesBeforeImport() {
    string profilesDirectory =
        ProfileStorageService.ProfilesDirectory;

    string[] currentProfiles =
        Directory.GetFiles(profilesDirectory,"*.json");

    if (currentProfiles.Length == 0) {
      return;
    }

    string safetyDirectory =
        Path.Combine(
            FileSystem.AppDataDirectory,
            "BeforeImport",
            DateTime.Now.ToString("yyyyMMdd_HHmmss",CultureInfo.InvariantCulture)
        );

    Directory.CreateDirectory(safetyDirectory);

    foreach (string profileFile in currentProfiles) {
      File.Copy(
          profileFile,
          Path.Combine(safetyDirectory,Path.GetFileName(profileFile)),
          overwrite: true
      );
    }
  }
}