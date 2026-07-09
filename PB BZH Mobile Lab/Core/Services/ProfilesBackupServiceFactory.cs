using PB_BZH_Mobile_Lab.Core.Services;

#if ANDROID
using PB_BZH_Mobile_Lab.Platforms.Android;
#endif

namespace PB_BZH_Mobile_Lab.UI.Services;

public static class ProfilesBackupServiceFactory {
  public static IProfilesBackupService? Create() {
#if ANDROID
    return new AndroidProfilesBackupService();
#else
    return null;
#endif
  }
}