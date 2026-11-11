namespace MovilidadUrbana.Web.Application.Resultados;

// Los tipos que salen del servicio de resultados son agregados: ninguno tiene `Nombre` ni `Edad`
// (REQ-IA-10). La prueba `Resultados_SinDatosPersonales` recorre por reflexión todo tipo
// alcanzable desde estos y falla si aparece uno de esos campos.

/// <summary>Una categoría de una distribución: clave del catálogo, etiqueta y cantidad.</summary>
public sealed record FilaDeDistribucion(string Clave, string Etiqueta, int Cantidad);

/// <summary>
/// Distribución de respuestas. <see cref="Sumable" /> dice si las cantidades son partes de un
/// todo (respuesta única) o no (multirrespuesta): de eso depende la forma del gráfico.
/// </summary>
public sealed record Distribucion(
    string Variable,
    int Encuestas,
    IReadOnlyList<FilaDeDistribucion> Filas,
    bool Sumable,
    string Nota)
{
    public int TotalDeMarcas => Filas.Sum(f => f.Cantidad);
}

/// <summary>Promedio y mediana de distancia y minutos de un grupo de respuestas.</summary>
public sealed record FilaDePromedio(
    string Grupo,
    string Etiqueta,
    int Encuestas,
    double DistanciaPromedio,
    double DistanciaMediana,
    double MinutosPromedio,
    double MinutosMediana);

public sealed record Promedios(string Por, IReadOnlyList<FilaDePromedio> Filas, string Nota);

/// <summary>Agrupación posible de los promedios.</summary>
public enum AgrupacionDePromedios { Ninguno, Medio, Localidad }
