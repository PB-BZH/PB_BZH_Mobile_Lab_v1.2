using PB_BZH_Mobile_Lab.Core.Services;

#if ANDROID
using PB_BZH_Mobile_Lab.Platforms.Android;
#endif

namespace PB_BZH_Mobile_Lab.UI;

public partial class AppShell: Shell {
  private readonly IProfilesBackupService? _profilesBackupService;

  public AppShell() {
    InitializeComponent();

#if ANDROID
    _profilesBackupService = new AndroidProfilesBackupService();
#endif
  }

  private async void OnExportProfilesClicked(
      object sender,
      EventArgs e) {

    if (_profilesBackupService is null) {
      await DisplayAlertAsync(
          "Sauvegarde",
          "L'export des profils n'est disponible que sur Android.",
          "OK"
      );

      return;
    }

    try {
      string fichier =
          await _profilesBackupService.ExporterAsync();

      await DisplayAlertAsync(
          "Sauvegarde terminée",
          "La sauvegarde des profils a été créée dans :" +
          Environment.NewLine +
          Environment.NewLine +
          fichier,
          "OK"
      );
    }
    catch (Exception ex) {
      await DisplayAlertAsync(
          "Erreur",
          "Impossible d'exporter les profils." +
          Environment.NewLine +
          Environment.NewLine +
          ex.Message,
          "OK"
      );
    }
  }

  private async void OnImportProfilesClicked(
      object sender,
      EventArgs e) {

    if (_profilesBackupService is null) {
      await DisplayAlertAsync(
          "Restauration",
          "L'import des profils n'est disponible que sur Android.",
          "OK"
      );

      return;
    }

    bool confirmer =
        await DisplayAlertAsync(
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

      if (Shell.Current.CurrentPage is MainPage mainPage) {
        await mainPage.RefreshProfilesAfterImportAsync();
      }

      await DisplayAlertAsync(
          "Import terminé",
          "Les profils ont été restaurés." +
          Environment.NewLine +
          Environment.NewLine +
          "Fermez puis relancez l'application pour garantir le rechargement complet.",
          "OK"
      );
    }
    catch (Exception ex) {
      await DisplayAlertAsync(
          "Erreur",
          "Impossible d'importer les profils." +
          Environment.NewLine +
          Environment.NewLine +
          ex.Message,
          "OK"
      );
    }
  }
}