using Android.App;
using Android.Runtime;
using PB_BZH_Mobile_Lab.UI;

namespace PB_BZH_Mobile_Lab;

[Application]
public class MainApplication: MauiApplication {
  public MainApplication(IntPtr handle,JniHandleOwnership ownership)
    : base(handle,ownership) {
  }

  protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
