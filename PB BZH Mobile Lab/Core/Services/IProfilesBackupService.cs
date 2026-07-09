namespace PB_BZH_Mobile_Lab.Core.Services;

public interface IProfilesBackupService {
  Task<string> ExporterAsync();

  Task ImporterAsync();
}