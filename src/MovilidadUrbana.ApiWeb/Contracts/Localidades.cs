namespace MovilidadUrbana.ApiWeb.Contracts;

/// <summary>Una localidad tal como la expone la API.</summary>
public sealed record LocalidadDto(int Id, string Nombre, string Provincia, string CodigoPostal, int Habitantes);

/// <summary>Lo que el cliente envía para dar de alta o modificar una localidad.</summary>
public sealed record LocalidadRequest(string? Nombre, string? Provincia, string? CodigoPostal, int? Habitantes);
