using PB_BZH_Mobile_Lab.UI.Services;

namespace PB_BZH_Mobile_Lab.UI;

public partial class AppShell: Shell {
  private readonly ProfilesBackupMenuController _profilesBackupMenuController;

  public AppShell() {
    InitializeComponent();

    _profilesBackupMenuController =
        new ProfilesBackupMenuController(
            this,
            ProfilesBackupServiceFactory.Create()
        );
  }

  private async void OnExportProfilesClicked(
      object sender,
      EventArgs e) {

    await _profilesBackupMenuController.ExportProfilesAsync();
  }

  private async void OnImportProfilesClicked(
      object sender,
      EventArgs e) {

    await _profilesBackupMenuController.ImportProfilesAsync();
  }
}