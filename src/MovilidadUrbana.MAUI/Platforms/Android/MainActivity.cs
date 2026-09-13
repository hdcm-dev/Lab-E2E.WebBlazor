using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;

namespace MovilidadUrbana.MAUI;

// Register: nombre estable de la activity, para que Appium pueda lanzarla (sin él, el nombre es un hash).
// AdjustResize: con el teclado abierto la ventana se achica, y la barra fija con «Guardar» o «Siguiente»
// queda visible encima del teclado en lugar de tapada por él.
[Register("ar.lab.movilidadurbana.MainActivity")]
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, WindowSoftInputMode = SoftInput.AdjustResize, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
