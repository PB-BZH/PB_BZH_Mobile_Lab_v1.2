using PB_BZH_Mobile_Lab.Core.Models;
using PB_BZH_Mobile_Lab.Core.Services;

namespace PB_BZH_Mobile_Lab;

public partial class MainPage: ContentPage {
  private readonly LicenseJsonService _licenseJsonService = new();
  private LicenseProfile _profile = new();
  private readonly ProfileStorageService _profileStorageService = new();
  private readonly LicenseFileService _licenseFileService = new();
  private string? _lastGeneratedLicenseFilePath;
  private List<LicenseProfile> _profiles = [];
  private bool _loadingProfiles;

  public MainPage() {
    InitializeComponent();

    chkValidUnlimited.CheckedChanged += (_,e) => dtpValidUntil.IsEnabled = !e.Value;
    chkMaintenanceUnlimited.CheckedChanged += (_,e) => dtpMaintenanceUntil.IsEnabled = !e.Value;
  }

  private void ApplyProfileToUI(
    LicenseProfile profile) {
    _profile = profile;
    txtProductId.Text = profile.ProductId;
    txtLicenseId.Text = profile.LicenseId;
    txtCustomerName.Text = profile.CustomerName;
    txtSite.Text = profile.Site;
    txtEmailContact.Text = profile.EmailContact;
    txtMachineHash.Text = profile.MachineHash;
    chkValidUnlimited.IsChecked = profile.ValidUntil is null;
    dtpValidUntil.IsEnabled = profile.ValidUntil is not null;
    dtpValidUntil.Date = (profile.ValidUntil ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue);
    chkMaintenanceUnlimited.IsChecked = profile.MaintenanceUntil is null;
    dtpMaintenanceUntil.IsEnabled = profile.MaintenanceUntil is not null;
    dtpMaintenanceUntil.Date = (profile.MaintenanceUntil ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue);
  }

  private LicenseProfile ApplyUIToProfile() {
    LicenseProfile profile = pickerProfiles.SelectedItem as LicenseProfile ?? new LicenseProfile();
    profile.ProductId = txtProductId.Text?.Trim() ?? string.Empty;
    profile.LicenseId = txtLicenseId.Text?.Trim() ?? string.Empty;
    profile.CustomerName = txtCustomerName.Text?.Trim() ?? string.Empty;
    profile.Site = txtSite.Text?.Trim() ?? string.Empty;
    profile.EmailContact = txtEmailContact.Text?.Trim() ?? string.Empty;
    profile.MachineHash = txtMachineHash.Text?.Trim() ?? string.Empty;
    profile.ValidUntil = chkValidUnlimited.IsChecked
      ? null
      : DateOnly.FromDateTime(dtpValidUntil.Date ?? DateTime.Today);
    profile.MaintenanceUntil = chkMaintenanceUnlimited.IsChecked
      ? null
      : DateOnly.FromDateTime(dtpMaintenanceUntil.Date ?? DateTime.Today);
    profile.ProfileName = ConstruireNomProfil(profile);

    return profile;
  }
  protected override async void OnAppearing() {
    base.OnAppearing();

    await LoadProfilesAsync();
  }

  private async Task LoadProfilesAsync(
    string? selectedProfileName = null) {

    _loadingProfiles = true;

    try {
      _profiles =
        await _profileStorageService.LoadProfilesAsync();

      pickerProfiles.ItemsSource = null;
      pickerProfiles.ItemsSource = _profiles;
      pickerProfiles.ItemDisplayBinding =
        new Binding(nameof(LicenseProfile.DisplayName));

      if (_profiles.Count == 0) {
        return;
      }

      int selectedIndex = 0;

      if (!string.IsNullOrWhiteSpace(selectedProfileName)) {
        int index = _profiles.FindIndex(p =>
          string.Equals(
            p.ProfileName,
            selectedProfileName,
            StringComparison.OrdinalIgnoreCase));

        if (index >= 0) {
          selectedIndex = index;
        }
      }

      pickerProfiles.SelectedIndex = selectedIndex;
      ApplyProfileToUI(_profiles[selectedIndex]);
    }
    finally {
      _loadingProfiles = false;
    }
  }

  private static string ConstruireNomProfil(
  LicenseProfile profile) {

    string client = profile.CustomerName?.Trim() ?? string.Empty;

    string produit = profile.ProductId?.Trim() ?? string.Empty;

    if (!string.IsNullOrWhiteSpace(client)
        && !string.IsNullOrWhiteSpace(produit)) {
      return $"{client} - {produit}";
    }

    if (!string.IsNullOrWhiteSpace(produit)) {
      return produit;
    }

    if (!string.IsNullOrWhiteSpace(client)) {
      return client;
    }

    return "Nouveau profil";
  }

  private async void BtnDeleteProfile_Clicked(
  object sender,
  EventArgs e) {

    if (pickerProfiles.SelectedItem is not LicenseProfile profile) {
      await DisplayAlertAsync(
        "Profil",
        "Aucun profil sélectionné.",
        "OK");

      return;
    }

    bool confirmation =
      await DisplayAlertAsync(
        "Supprimer le profil",
        $"Supprimer le profil \"{profile.DisplayName}\" ?",
        "Supprimer",
        "Annuler");

    if (!confirmation) {
      return;
    }

    _profileStorageService.DeleteProfile(profile);

    txtResult.Text = string.Empty;
    txtResult.IsVisible = false;
    btnToggleResult.Text = "Afficher le JSON";

    await LoadProfilesAsync();
  }

  private async void BtnLoadProfile_Clicked(object sender,EventArgs e) {
    await LoadProfilesAsync();
    if (_profiles.Count == 0) {
      await DisplayAlertAsync("Profil","Aucun profil enregistré.","OK");
      return;
    }
    pickerProfiles.Focus();
  }

  private async void BtnImportPrivateKey_Clicked(
  object sender,
  EventArgs e) {

    FileResult? result =
      await FilePicker.Default.PickAsync(new PickOptions {
        PickerTitle = "Sélectionner private_key.pem"
      });

    if (result is null) {
      return;
    }

    await using Stream sourceStream =
      await result.OpenReadAsync();

    await using FileStream targetStream =
      File.Create(LicenseFileService.GetPrivateKeyPath());

    await sourceStream.CopyToAsync(targetStream);

    await DisplayAlertAsync(
      "Clé privée",
      "La clé privée a été importée.",
      "OK");
  }

  private void PickerProfiles_SelectedIndexChanged(
  object sender,
  EventArgs e) {

    if (_loadingProfiles
        || pickerProfiles.SelectedItem is not LicenseProfile profile) {
      return;
    }

    ApplyProfileToUI(profile);
  }

  private static LicenseProfile CreateEmptyProfile() {
    DateOnly today =
      DateOnly.FromDateTime(DateTime.Today);

    return new LicenseProfile {
      ProductId = string.Empty,
      LicenseId = string.Empty,
      CustomerName = string.Empty,
      EmailContact = string.Empty,
      MachineHash = string.Empty,
      ValidUntil = today.AddYears(1),
      MaintenanceUntil = today.AddYears(1)
    };
  }

  private async void BtnGenerateLicense_Clicked(
    object sender,
    EventArgs e) {

    try {
      LicenseProfile profile =
        ApplyUIToProfile();

      await _profileStorageService.SaveProfileAsync(profile);

      await LoadProfilesAsync(profile.ProfileName);

      _lastGeneratedLicenseFilePath =
        await _licenseFileService.GenerateLicenseFileAsync(profile);

      txtResult.Text =
        await File.ReadAllTextAsync(_lastGeneratedLicenseFilePath);

      txtResult.IsVisible = true;
      btnToggleResult.Text = "Masquer le JSON";

      await DisplayAlertAsync(
        "Licence",
        "La licence a été générée.",
        "OK");
    }
    catch (Exception exception) {
      await DisplayAlertAsync(
        "Génération impossible",
        exception.Message,
        "OK");
    }
  }

  private async void BtnSaveProfile_Clicked(
    object sender,
    EventArgs e) {

    LicenseProfile profile =
      ApplyUIToProfile();

    await _profileStorageService.SaveProfileAsync(profile);

    await LoadProfilesAsync(profile.ProfileName);

    await DisplayAlertAsync(
      "Profil",
      "Le profil a été enregistré.",
      "OK");
  }

  private void BtnNewProfile_Clicked(object sender,EventArgs e) {
    ApplyProfileToUI(CreateEmptyProfile());
  }

  private void BtnToggleResult_Clicked(object? sender,EventArgs e) {
    txtResult.IsVisible = !txtResult.IsVisible;
    btnToggleResult.Text = txtResult.IsVisible
      ? "Masquer JSON"
      : "JSON";
  }

  private async void BtnShareLicense_Clicked(object sender,EventArgs e) {
    if (string.IsNullOrWhiteSpace(_lastGeneratedLicenseFilePath) || !File.Exists(_lastGeneratedLicenseFilePath)) {
      LicenseProfile profile = ApplyUIToProfile();
      _lastGeneratedLicenseFilePath = await _licenseFileService.GenerateLicenseFileAsync(profile);
    }

    await Share.Default.RequestAsync(new ShareFileRequest {
      Title = "Partager la licence",
      File = new ShareFile(_lastGeneratedLicenseFilePath)
    });
  }
}