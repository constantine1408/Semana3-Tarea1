using ClientesWeb.Models;

namespace ClientesWeb.Repositories;

/// <summary>Contrato del CRUD de clientes. El controlador depende de esta interfaz, no de MySQL.</summary>
public interface IClienteRepository
{
    /// <summary>CREATE. Guarda el cliente y devuelve el id generado.</summary>
    /// <exception cref="ClienteDuplicadoException">Si ya existe un cliente con esa cédula.</exception>
    Task<int> CrearAsync(Cliente cliente);

    /// <summary>READ. Devuelve el cliente o null si no existe.</summary>
    Task<Cliente?> ObtenerAsync(int id);

    /// <summary>READ. Lista los clientes; si hay texto, filtra por cédula, nombres, apellidos, correo o ciudad.</summary>
    Task<IReadOnlyList<Cliente>> ListarAsync(string? busqueda);

    /// <summary>UPDATE.</summary>
    /// <exception cref="ClienteNoEncontradoException">Si el id no existe.</exception>
    /// <exception cref="ClienteDuplicadoException">Si la cédula pertenece a otro cliente.</exception>
    Task ActualizarAsync(Cliente cliente);

    /// <summary>DELETE.</summary>
    /// <exception cref="ClienteNoEncontradoException">Si el id no existe.</exception>
    Task EliminarAsync(int id);
}
