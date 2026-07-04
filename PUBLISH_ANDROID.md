# Publication Android

## Méthode officielle

La publication finale de l'APK se fait avec `dotnet publish`.

Le flux Visual Studio `Publier / Distribuer` génère actuellement un APK refusé par Android avec l'erreur :

`resources.arsc must be stored uncompressed and aligned on a 4-byte boundary`

## Commande

```powershell
dotnet publish ".\PB BZH Mobile Lab\PB BZH Mobile Lab.csproj" `
  -f net10.0-android `
  -c Release `
  -p:AndroidPackageFormats=apk `
  -p:AndroidKeyStore=true `
  -p:AndroidSigningKeyStore="CHEMIN\pb-bzh-mobile-lab.keystore" `
  -p:AndroidSigningKeyAlias="pb-bzh-mobile-lab" `
  -p:AndroidSigningKeyPass="MOT_DE_PASSE" `
  -p:AndroidSigningStorePass="MOT_DE_PASSE"