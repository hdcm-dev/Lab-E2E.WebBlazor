namespace MovilidadUrbana.ApiWeb.Contratos;

/// <summary>Una localidad tal como la expone la API.</summary>
public sealed record LocalidadDto(int Id, string Nombre, string Provincia, string CodigoPostal, int Habitantes);

/// <summary>Lo que el cliente envía para dar de alta o modificar una localidad.</summary>
public sealed record SolicitudDeLocalidad(string? Nombre, string? Provincia, string? CodigoPostal, int? Habitantes);
