namespace ClientesWeb.Repositories;

/// <summary>El procedimiento almacenado informó que el cliente no existe (SQLSTATE 45404).</summary>
public class ClienteNoEncontradoException : Exception
{
    public ClienteNoEncontradoException(string mensaje) : base(mensaje) { }
}

/// <summary>Ya existe un cliente con la misma cédula (SQLSTATE 45000 o clave duplicada).</summary>
public class ClienteDuplicadoException : Exception
{
    public ClienteDuplicadoException(string mensaje) : base(mensaje) { }
}
