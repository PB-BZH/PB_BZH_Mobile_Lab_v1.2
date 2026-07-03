using System.Globalization;
using Microsoft.Extensions.Logging;

namespace PB_BZH_Mobile_Lab;

public static class MauiProgram {
  public static MauiApp CreateMauiApp() {
    var builder = MauiApp.CreateBuilder();
    builder
      .UseMauiApp<App>()
      .ConfigureFonts(fonts => {
        fonts.AddFont("OpenSans-Regular.ttf","OpenSansRegular");
        fonts.AddFont("OpenSans-Semibold.ttf","OpenSansSemibold");
      });

#if DEBUG
    builder.Logging.AddDebug();
#endif
    CultureInfo frenchCulture = new("fr-FR");
    CultureInfo.DefaultThreadCurrentCulture = frenchCulture;
    CultureInfo.DefaultThreadCurrentUICulture = frenchCulture;
    return builder.Build();
  }
}
