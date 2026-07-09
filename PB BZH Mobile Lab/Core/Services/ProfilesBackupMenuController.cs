using PB_BZH_Mobile_Lab.Core.Services;

namespace PB_BZH_Mobile_Lab.UI.Services;

public sealed class ProfilesBackupMenuController {
  private readonly Shell _shell;
  private readonly IProfilesBackupService? _profilesBackupService;

  public ProfilesBackupMenuController(
      Shell shell,
      IProfilesBackupService? profilesBackupService) {

    _shell = shell;
    _profilesBackupService = profilesBackupService;
  }

  public async Task ExportProfilesAsync() {
    if (_profilesBackupService is null) {
      await _shell.DisplayAlertAsync(
          "Sauvegarde",
          "L'export des profils n'est disponible que sur Android.",
          "OK"
      );

      return;
    }

    try {
      string fichier =
          await _profilesBackupService.ExporterAsync();

      await _shell.DisplayAlertAsync(
          "Sauvegarde terminée",
          "La sauvegarde des profils a été créée dans :" +
          Environment.NewLine +
          Environment.NewLine +
          fichier,
          "OK"
      );
    }
    catch (Exception ex) {
      await _shell.DisplayAlertAsync(
          "Erreur",
          "Impossible d'exporter les profils." +
          Environment.NewLine +
          Environment.NewLine +
          ex.Message,
          "OK"
      );
    }
  }

  public async Task ImportProfilesAsync() {
    if (_profilesBackupService is null) {
      await _shell.DisplayAlertAsync(
          "Restauration",
          "L'import des profils n'est disponible que sur Android.",
          "OK"
      );

      return;
    }

    bool confirmer =
        await _shell.DisplayAlertAsync(
            "Importer les profils",
            "L'import va remplacer les profils actuellement enregistrés." +
            Environment.NewLine +
            Environment.NewLine +
            "La clé privée RSA ne sera pas importée." +
            Environment.NewLine +
            Environment.NewLine +
            "Continuer ?",
            "Importer",
            "Annuler"
        );

    if (!confirmer) {
      return;
    }

    try {
      await _profilesBackupService.ImporterAsync();

      await RefreshMainPageAsync();

      await _shell.DisplayAlertAsync(
          "Import terminé",
          "Les profils ont été restaurés.",
          "OK"
      );
    }
    catch (Exception ex) {
      await _shell.DisplayAlertAsync(
          "Erreur",
          "Impossible d'importer les profils." +
          Environment.NewLine +
          Environment.NewLine +
          ex.Message,
          "OK"
      );
    }
  }

  private static async Task RefreshMainPageAsync() {
    if (Shell.Current.CurrentPage is MainPage mainPage) {
      await mainPage.RefreshProfilesAfterImportAsync();
    }
  }
}