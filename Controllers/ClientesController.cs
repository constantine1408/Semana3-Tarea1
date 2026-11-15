using ClientesWeb.Models;
using ClientesWeb.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ClientesWeb.Controllers;

/// <summary>
/// API REST del CRUD de clientes.
///
///   GET    /api/clientes?q=texto   → lista (con búsqueda opcional)
///   GET    /api/clientes/{id}      → un cliente
///   POST   /api/clientes           → crear
///   PUT    /api/clientes/{id}      → actualizar
///   DELETE /api/clientes/{id}      → eliminar
///
/// [ApiController] valida el modelo antes de entrar a cada acción
/// y responde 400 automáticamente si hay errores.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IClienteRepository _repositorio;

    public ClientesController(IClienteRepository repositorio)
    {
        _repositorio = repositorio;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cliente>>> Listar([FromQuery] string? q)
    {
        return Ok(await _repositorio.ListarAsync(q));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Cliente>> Obtener(int id)
    {
        var cliente = await _repositorio.ObtenerAsync(id);
        return cliente is null ? NoEncontrado() : Ok(cliente);
    }

    [HttpPost]
    public async Task<ActionResult<Cliente>> Crear([FromBody] Cliente cliente)
    {
        cliente.Normalizar();

        try
        {
            var id = await _repositorio.CrearAsync(cliente);
            var creado = await _repositorio.ObtenerAsync(id);
            return CreatedAtAction(nameof(Obtener), new { id }, creado);
        }
        catch (ClienteDuplicadoException ex)
        {
            return Conflicto(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Cliente>> Actualizar(int id, [FromBody] Cliente cliente)
    {
        cliente.Id = id; // el id manda la URL, no el cuerpo
        cliente.Normalizar();

        try
        {
            await _repositorio.ActualizarAsync(cliente);
            return Ok(await _repositorio.ObtenerAsync(id));
        }
        catch (ClienteNoEncontradoException)
        {
            return NoEncontrado();
        }
        catch (ClienteDuplicadoException ex)
        {
            return Conflicto(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await _repositorio.EliminarAsync(id);
            return NoContent();
        }
        catch (ClienteNoEncontradoException)
        {
            return NoEncontrado();
        }
    }

    // ---------------------------------------------------------------- ayudas
    private ObjectResult NoEncontrado() =>
        NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "No encontrado",
            Detail = "No se encontró el cliente solicitado.",
        });

    private ObjectResult Conflicto(string detalle) =>
        Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflicto",
            Detail = detalle,
        });
}
