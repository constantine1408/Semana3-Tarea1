using System.Data;
using System.Data.Common;
using ClientesWeb.Models;
using MySqlConnector;

namespace ClientesWeb.Repositories;

/// <summary>
/// Implementación MySQL/MariaDB del CRUD. No contiene SQL: cada operación
/// invoca un procedimiento almacenado (sql/esquema.sql) con parámetros tipados,
/// lo que además evita la inyección SQL.
/// </summary>
public class ClienteRepository : IClienteRepository
{
    private const string SqlStateConflicto = "45000";
    private const string SqlStateNoEncontrado = "45404";

    private readonly string _cadenaConexion;

    public ClienteRepository(IConfiguration configuracion)
    {
        _cadenaConexion = configuracion.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'Default' en appsettings.json.");
    }

    // ----------------------------------------------------------------- CREATE
    public async Task<int> CrearAsync(Cliente cliente)
    {
        try
        {
            await using var conexion = await AbrirAsync();
            await using var cmd = Procedimiento(conexion, "sp_cliente_crear");
            AgregarDatos(cmd, cliente);

            // El procedimiento termina con SELECT LAST_INSERT_ID(): primera columna de la primera fila.
            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }
        catch (MySqlException ex)
        {
            throw Traducir(ex);
        }
    }

    // ------------------------------------------------------------------- READ
    public async Task<Cliente?> ObtenerAsync(int id)
    {
        await using var conexion = await AbrirAsync();
        await using var cmd = Procedimiento(conexion, "sp_cliente_obtener");
        cmd.Parameters.AddWithValue("p_id", id);

        await using var lector = await cmd.ExecuteReaderAsync();
        return await lector.ReadAsync() ? Mapear(lector) : null;
    }

    public async Task<IReadOnlyList<Cliente>> ListarAsync(string? busqueda)
    {
        await using var conexion = await AbrirAsync();
        await using var cmd = Procedimiento(conexion, "sp_cliente_listar");

        if (string.IsNullOrWhiteSpace(busqueda))
            cmd.Parameters.AddWithValue("p_busqueda", DBNull.Value);
        else
            cmd.Parameters.AddWithValue("p_busqueda", busqueda.Trim());

        var clientes = new List<Cliente>();
        await using var lector = await cmd.ExecuteReaderAsync();
        while (await lector.ReadAsync())
            clientes.Add(Mapear(lector));

        return clientes;
    }

    // ----------------------------------------------------------------- UPDATE
    public async Task ActualizarAsync(Cliente cliente)
    {
        try
        {
            await using var conexion = await AbrirAsync();
            await using var cmd = Procedimiento(conexion, "sp_cliente_actualizar");
            cmd.Parameters.AddWithValue("p_id", cliente.Id);
            AgregarDatos(cmd, cliente);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (MySqlException ex)
        {
            throw Traducir(ex);
        }
    }

    // ----------------------------------------------------------------- DELETE
    public async Task EliminarAsync(int id)
    {
        try
        {
            await using var conexion = await AbrirAsync();
            await using var cmd = Procedimiento(conexion, "sp_cliente_eliminar");
            cmd.Parameters.AddWithValue("p_id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (MySqlException ex)
        {
            throw Traducir(ex);
        }
    }

    // --------------------------------------------------------------- privados
    private async Task<MySqlConnection> AbrirAsync()
    {
        var conexion = new MySqlConnection(_cadenaConexion);
        await conexion.OpenAsync();
        return conexion;
    }

    private static MySqlCommand Procedimiento(MySqlConnection conexion, string nombre) =>
        new(nombre, conexion) { CommandType = CommandType.StoredProcedure };

    /// <summary>Parámetros comunes a crear y actualizar (los nombres coinciden con el procedimiento).</summary>
    private static void AgregarDatos(MySqlCommand cmd, Cliente c)
    {
        cmd.Parameters.AddWithValue("p_cedula", c.Cedula);
        cmd.Parameters.AddWithValue("p_nombres", c.Nombres);
        cmd.Parameters.AddWithValue("p_apellidos", c.Apellidos);
        cmd.Parameters.AddWithValue("p_fecha_nacimiento", c.FechaNacimiento.Date);
        cmd.Parameters.AddWithValue("p_genero", c.Genero);
        cmd.Parameters.AddWithValue("p_email", c.Email);
        cmd.Parameters.AddWithValue("p_telefono", c.Telefono);
        cmd.Parameters.AddWithValue("p_direccion", c.Direccion ?? "");
        cmd.Parameters.AddWithValue("p_ciudad", c.Ciudad);
        cmd.Parameters.AddWithValue("p_provincia", c.Provincia);
        cmd.Parameters.AddWithValue("p_activo", c.Activo ? 1 : 0);
    }

    /// <summary>Convierte la fila actual en un objeto Cliente.</summary>
    private static Cliente Mapear(DbDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("id")),
        Cedula = r.GetString(r.GetOrdinal("cedula")),
        Nombres = r.GetString(r.GetOrdinal("nombres")),
        Apellidos = r.GetString(r.GetOrdinal("apellidos")),
        FechaNacimiento = r.GetDateTime(r.GetOrdinal("fecha_nacimiento")),
        Genero = r.GetString(r.GetOrdinal("genero")),
        Email = r.GetString(r.GetOrdinal("email")),
        Telefono = r.GetString(r.GetOrdinal("telefono")),
        Direccion = r.GetString(r.GetOrdinal("direccion")),
        Ciudad = r.GetString(r.GetOrdinal("ciudad")),
        Provincia = r.GetString(r.GetOrdinal("provincia")),
        Activo = r.GetBoolean(r.GetOrdinal("activo")),
        FechaRegistro = r.GetDateTime(r.GetOrdinal("fecha_registro")),
    };

    /// <summary>Convierte los errores del procedimiento almacenado en excepciones del dominio.</summary>
    private static Exception Traducir(MySqlException ex)
    {
        if (ex.SqlState == SqlStateNoEncontrado)
            return new ClienteNoEncontradoException(ex.Message);

        if (ex.SqlState == SqlStateConflicto)
            return new ClienteDuplicadoException(ex.Message);

        // Respaldo: si dos peticiones simultáneas pasan la verificación, la clave única lo detecta.
        if (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
            return new ClienteDuplicadoException("Ya existe un cliente con esa cédula.");

        return ex;
    }
}
