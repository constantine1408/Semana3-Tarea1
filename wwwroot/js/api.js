// Capa de acceso a la API REST (/api/clientes). Toda la comunicación con el servidor va por aqui.

const BASE = '/api/clientes';

/* Error de la API con una lista de mensajes listos para mostrar al usuario. */
export class ApiError extends Error {
  constructor(mensajes, estado) {
    super(mensajes.join(' '));
    this.mensajes = mensajes;
    this.estado = estado;
  }
}

async function solicitar(url, opciones = {}) {
  let respuesta;
  try {
    respuesta = await fetch(url, {
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json',
      },
      ...opciones,
    });
  } catch {
    throw new ApiError(
      [
        'No se pudo conectar con el servidor. Verifique que la aplicación esté en ejecución.',
      ],
      0,
    );
  }

  if (respuesta.status === 204) return null;

  let cuerpo = null;
  const texto = await respuesta.text();
  if (texto) {
    try {
      cuerpo = JSON.parse(texto);
    } catch {
      cuerpo = null;
    }
  }

  if (!respuesta.ok)
    throw new ApiError(
      extraerMensajes(cuerpo, respuesta.status),
      respuesta.status,
    );
  return cuerpo;
}

/* Convierte las respuestas de error de ASP.NET (ProblemDetails) en una lista de textos. */
function extraerMensajes(cuerpo, estado) {
  if (estado >= 500) {
    return [
      'Ocurrió un error en el servidor. Revise que MySQL esté iniciado y que el esquema (sql/esquema.sql) esté importado.',
    ];
  }
  if (cuerpo?.errors) return Object.values(cuerpo.errors).flat(); // validación (400)
  if (cuerpo?.detail) return [cuerpo.detail]; // conflicto (409), no encontrado (404)
  if (cuerpo?.title) return [cuerpo.title];
  return [`Error inesperado (código ${estado}).`];
}

export const listar = (texto = '') =>
  solicitar(texto ? `${BASE}?q=${encodeURIComponent(texto)}` : BASE);

export const obtener = (id) => solicitar(`${BASE}/${id}`);

export const crear = (datos) =>
  solicitar(BASE, { method: 'POST', body: JSON.stringify(datos) });

export const actualizar = (id, datos) =>
  solicitar(`${BASE}/${id}`, { method: 'PUT', body: JSON.stringify(datos) });

export const eliminar = (id) =>
  solicitar(`${BASE}/${id}`, { method: 'DELETE' });
