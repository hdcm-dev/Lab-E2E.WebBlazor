namespace MovilidadUrbana.MAUI.Servicios;

/// <summary>
/// Mantiene una barra fija encima del teclado en pantalla. En las páginas raíz de las pestañas alcanza
/// con <c>AdjustResize</c>; en una página apilada por Shell la ventana no se achica (verificado en el
/// moto e6 play), <c>SafeAreaEdges</c> deja un hueco que no se cierra al ocultar el teclado, y los
/// insets del IME no llegan a la vista de la página. Lo que sí es estable es medir, en cada
/// redistribución de la ventana, si el borde inferior de la barra quedó debajo de la zona visible, y
/// corregir su margen por esa diferencia.
/// </summary>
public static class TecladoEnPantalla
{
    public static void MantenerVisible(ContentPage pagina, View barra)
    {
#if ANDROID
        Android.Views.View? decor = null;
        EventHandler? alRedistribuir = null;

        pagina.Appearing += (_, _) =>
        {
            decor = Platform.CurrentActivity?.Window?.DecorView;
            if (decor is null) return;
            alRedistribuir = (_, _) =>
            {
                if (barra.Handler?.PlatformView is not Android.Views.View vistaDeBarra) return;
                var visible = new Android.Graphics.Rect();
                decor.GetWindowVisibleDisplayFrame(visible);
                var posicion = new int[2];
                vistaDeBarra.GetLocationOnScreen(posicion);
                // Cuánto del borde inferior de la barra queda debajo de lo visible (positivo: tapado por el
                // teclado; negativo: sobra margen). Se corrige el margen por esa diferencia hasta converger, y
                // así no importa quién más haya acolchado el contenido: solo cuenta dónde quedó la barra.
                var desvio = posicion[1] + vistaDeBarra.Height - visible.Bottom;
                var densidad = decor.Context?.Resources?.DisplayMetrics?.Density ?? 1;
                var margen = Math.Max(0, barra.Margin.Bottom + desvio / densidad);
                if (Math.Abs(barra.Margin.Bottom - margen) > 1) barra.Margin = new Thickness(0, 0, 0, margen);
            };
            decor.ViewTreeObserver!.GlobalLayout += alRedistribuir;
        };

        pagina.Disappearing += (_, _) =>
        {
            if (decor is not null && alRedistribuir is not null) decor.ViewTreeObserver!.GlobalLayout -= alRedistribuir;
            barra.Margin = Thickness.Zero;
        };
#endif
    }
}
