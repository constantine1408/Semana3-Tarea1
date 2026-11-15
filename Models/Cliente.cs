using System.ComponentModel.DataAnnotations;

namespace ClientesWeb.Models;

/// <summary>
/// Entidad Cliente (13 campos). Las reglas de validación viven en el propio objeto:
/// ASP.NET Core las ejecuta automáticamente al recibir el JSON y responde 400
/// con la lista de errores si algún dato no es válido.
/// </summary>
public class Cliente : IValidatableObject
{
    // 1
    public int Id { get; set; }

    // 2
    [Required(ErrorMessage = "La cédula es obligatoria.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "La cédula debe tener 10 dígitos numéricos.")]
    public string Cedula { get; set; } = "";

    // 3
    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    [StringLength(80, ErrorMessage = "Los nombres no pueden superar los 80 caracteres.")]
    public string Nombres { get; set; } = "";

    // 4
    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(80, ErrorMessage = "Los apellidos no pueden superar los 80 caracteres.")]
    public string Apellidos { get; set; } = "";

    // 5
    public DateTime FechaNacimiento { get; set; }

    // 6
    [Required(ErrorMessage = "El género es obligatorio.")]
    [RegularExpression("^[MFO]$", ErrorMessage = "El género debe ser M (masculino), F (femenino) u O (otro).")]
    public string Genero { get; set; } = "";

    // 7
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
    public string Email { get; set; } = "";

    // 8
    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [RegularExpression(@"^\d{7,10}$", ErrorMessage = "El teléfono debe tener entre 7 y 10 dígitos.")]
    public string Telefono { get; set; } = "";

    // 9 (opcional)
    [StringLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
    public string? Direccion { get; set; }

    // 10
    [Required(ErrorMessage = "La ciudad es obligatoria.")]
    [StringLength(80, ErrorMessage = "La ciudad no puede superar los 80 caracteres.")]
    public string Ciudad { get; set; } = "";

    // 11
    [Required(ErrorMessage = "La provincia es obligatoria.")]
    [StringLength(80, ErrorMessage = "La provincia no puede superar los 80 caracteres.")]
    public string Provincia { get; set; } = "";

    // 12
    public bool Activo { get; set; } = true;

    // 13 (la asigna la base de datos; el cliente no la envía)
    public DateTime FechaRegistro { get; set; }

    /// <summary>Solo lectura: se envía al navegador para facilitar la pantalla.</summary>
    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();

    /// <summary>Elimina espacios sobrantes antes de guardar.</summary>
    public void Normalizar()
    {
        Cedula = Cedula.Trim();
        Nombres = Nombres.Trim();
        Apellidos = Apellidos.Trim();
        Genero = Genero.Trim().ToUpperInvariant();
        Email = Email.Trim();
        Telefono = Telefono.Trim();
        Direccion = (Direccion ?? "").Trim();
        Ciudad = Ciudad.Trim();
        Provincia = Provincia.Trim();
    }

    /// <summary>Reglas que involucran más de una condición (se ejecutan si las anteriores pasan).</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaNacimiento == default)
        {
            yield return new ValidationResult(
                "La fecha de nacimiento es obligatoria.", new[] { nameof(FechaNacimiento) });
        }
        else if (FechaNacimiento.Date > DateTime.Today)
        {
            yield return new ValidationResult(
                "La fecha de nacimiento no puede ser futura.", new[] { nameof(FechaNacimiento) });
        }
        else if (FechaNacimiento.Date < DateTime.Today.AddYears(-120))
        {
            yield return new ValidationResult(
                "La fecha de nacimiento no es válida.", new[] { nameof(FechaNacimiento) });
        }
    }
}
