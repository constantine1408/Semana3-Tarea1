// Lógica de la pantalla: tabla, búsqueda, formulario (crear/editar), detalle y eliminación el CRUD
// Todo el texto proveniente del servidor se inserta con textContent (nunca innerHTML) → sin XSS.

import { ApiError, actualizar, crear, eliminar, listar } from './api.js';

const PROVINCIAS = [
  'Azuay',
  'Bolívar',
  'Cañar',
  'Carchi',
  'Chimborazo',
  'Cotopaxi',
  'El Oro',
  'Esmeraldas',
  'Galápagos',
  'Guayas',
  'Imbabura',
  'Loja',
  'Los Ríos',
  'Manabí',
  'Morona Santiago',
  'Napo',
  'Orellana',
  'Pastaza',
  'Pichincha',
  'Santa Elena',
  'Santo Domingo de los Tsáchilas',
  'Sucumbíos',
  'Tungurahua',
  'Zamora Chinchipe',
];
const GENEROS = { M: 'Masculino', F: 'Femenino', O: 'Otro' };

const $ = (selector) => document.querySelector(selector);
const el = {
  tbody: $('#tbody'),
  contador: $('#contador'),
  aviso: $('#aviso'),
  busqueda: $('#busqueda'),
  formBusqueda: $('#form-busqueda'),
  btnNuevo: $('#btn-nuevo'),

  dlgForm: $('#dlg-form'),
  form: $('#form-cliente'),
  formTitulo: $('#form-titulo'),
  formErrores: $('#form-errores'),
  btnGuardar: $('#btn-guardar'),
  btnCancelar: $('#btn-cancelar'),

  dlgDetalle: $('#dlg-detalle'),
  detalleLista: $('#detalle-lista'),
  detalleCerrar: $('#detalle-cerrar'),
  detalleEditar: $('#detalle-editar'),

  dlgEliminar: $('#dlg-eliminar'),
  eliminarTexto: $('#eliminar-texto'),
  eliminarCancelar: $('#eliminar-cancelar'),
  eliminarConfirmar: $('#eliminar-confirmar'),
};

let clientes = [];
let idEnEdicion = null; // null = creando
let clienteParaEliminar = null;
let clienteEnDetalle = null;
let temporizadorBusqueda;
let temporizadorAviso;

/* utilidades*/

function crearElemento(etiqueta, texto, clase) {
  const nodo = document.createElement(etiqueta);
  if (texto !== undefined) nodo.textContent = texto;
  if (clase) nodo.className = clase;
  return nodo;
}

/* "2000-05-10T00:00:00" → "10 de mayo de 2000" (sin desfase de zona horaria). */
function formatearFecha(iso) {
  if (!iso) return '—';
  const [anio, mes, dia] = iso.slice(0, 10).split('-').map(Number);
  return new Date(anio, mes - 1, dia).toLocaleDateString('es-EC', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
}

function formatearFechaHora(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleString('es-EC', {
    dateStyle: 'medium',
    timeStyle: 'short',
  });
}

function mostrarAviso(mensaje, tipo = 'ok') {
  clearTimeout(temporizadorAviso);
  el.aviso.textContent = mensaje;
  el.aviso.className = `aviso aviso--${tipo}`;
  el.aviso.hidden = false;
  temporizadorAviso = setTimeout(
    () => (el.aviso.hidden = true),
    tipo === 'ok' ? 4000 : 8000,
  );
}

function mensajesDe(error) {
  return error instanceof ApiError
    ? error.mensajes
    : ['Ocurrió un error inesperado.'];
}

/* tabla*/

async function cargar() {
  try {
    clientes = await listar(el.busqueda.value.trim());
    dibujarTabla();
  } catch (error) {
    clientes = [];
    dibujarTabla();
    mostrarAviso(mensajesDe(error).join(' '), 'error');
  }
}

function dibujarTabla() {
  el.tbody.replaceChildren();

  const hayBusqueda = el.busqueda.value.trim() !== '';
  el.contador.textContent =
    clientes.length === 1 ? '1 cliente' : `${clientes.length} clientes`;

  if (clientes.length === 0) {
    const fila = crearElemento('tr');
    const celda = crearElemento(
      'td',
      hayBusqueda
        ? 'Ningún cliente coincide con la búsqueda.'
        : 'Aún no hay clientes. Use «Nuevo cliente» para registrar el primero.',
      'vacio',
    );
    celda.colSpan = 7;
    fila.append(celda);
    el.tbody.append(fila);
    return;
  }

  for (const cliente of clientes) el.tbody.append(crearFila(cliente));
}

function crearFila(cliente) {
  const fila = crearElemento('tr');

  fila.append(crearElemento('td', cliente.cedula, 'numero'));

  const celdaNombre = crearElemento('td');
  const botonNombre = crearElemento(
    'button',
    cliente.nombreCompleto,
    'enlace enlace--nombre',
  );
  botonNombre.type = 'button';
  botonNombre.title = 'Ver detalle';
  botonNombre.addEventListener('click', () => abrirDetalle(cliente));
  celdaNombre.append(botonNombre);
  fila.append(celdaNombre);

  fila.append(crearElemento('td', cliente.email));
  fila.append(crearElemento('td', cliente.telefono, 'numero'));
  fila.append(crearElemento('td', cliente.ciudad));

  const celdaEstado = crearElemento('td');
  celdaEstado.append(
    crearElemento(
      'span',
      cliente.activo ? 'Activo' : 'Inactivo',
      `insignia ${cliente.activo ? 'insignia--activo' : 'insignia--inactivo'}`,
    ),
  );
  fila.append(celdaEstado);

  const celdaAcciones = crearElemento('td', undefined, 'acciones-fila');
  const botonEditar = crearElemento('button', 'Editar', 'enlace');
  botonEditar.type = 'button';
  botonEditar.setAttribute('aria-label', `Editar a ${cliente.nombreCompleto}`);
  botonEditar.addEventListener('click', () => abrirFormulario(cliente));

  const botonEliminar = crearElemento(
    'button',
    'Eliminar',
    'enlace enlace--peligro',
  );
  botonEliminar.type = 'button';
  botonEliminar.setAttribute(
    'aria-label',
    `Eliminar a ${cliente.nombreCompleto}`,
  );
  botonEliminar.addEventListener('click', () => abrirEliminar(cliente));

  celdaAcciones.append(botonEditar, botonEliminar);
  fila.append(celdaAcciones);

  return fila;
}

/* formulario*/

function abrirFormulario(cliente = null) {
  idEnEdicion = cliente?.id ?? null;
  el.formTitulo.textContent = cliente ? 'Editar cliente' : 'Nuevo cliente';
  el.btnGuardar.textContent = cliente ? 'Guardar cambios' : 'Registrar cliente';
  ocultarErroresFormulario();

  const f = el.form.elements;
  f.cedula.value = cliente?.cedula ?? '';
  f.nombres.value = cliente?.nombres ?? '';
  f.apellidos.value = cliente?.apellidos ?? '';
  f.fechaNacimiento.value = cliente?.fechaNacimiento?.slice(0, 10) ?? '';
  f.genero.value = cliente?.genero ?? '';
  f.email.value = cliente?.email ?? '';
  f.telefono.value = cliente?.telefono ?? '';
  f.direccion.value = cliente?.direccion ?? '';
  f.ciudad.value = cliente?.ciudad ?? '';
  f.provincia.value = cliente?.provincia ?? '';
  f.activo.checked = cliente?.activo ?? true;

  if (el.dlgDetalle.open) el.dlgDetalle.close();
  el.dlgForm.showModal();
  f.cedula.focus();
}

function leerFormulario() {
  const f = el.form.elements;
  return {
    cedula: f.cedula.value.trim(),
    nombres: f.nombres.value.trim(),
    apellidos: f.apellidos.value.trim(),
    fechaNacimiento: f.fechaNacimiento.value,
    genero: f.genero.value,
    email: f.email.value.trim(),
    telefono: f.telefono.value.trim(),
    direccion: f.direccion.value.trim(),
    ciudad: f.ciudad.value.trim(),
    provincia: f.provincia.value.trim(),
    activo: f.activo.checked,
  };
}

/* Misma validación que el servidor, para avisar de inmediato. El servidor sigue siendo la autoridad. */
function validar(d) {
  const errores = [];
  if (!/^\d{10}$/.test(d.cedula))
    errores.push('La cédula debe tener 10 dígitos numéricos.');
  if (!d.nombres) errores.push('Los nombres son obligatorios.');
  if (!d.apellidos) errores.push('Los apellidos son obligatorios.');

  if (!d.fechaNacimiento) {
    errores.push('La fecha de nacimiento es obligatoria.');
  } else if (d.fechaNacimiento > new Date().toLocaleDateString('sv-SE')) {
    errores.push('La fecha de nacimiento no puede ser futura.');
  }

  if (!d.genero) errores.push('Seleccione el género.');
  if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(d.email))
    errores.push('El correo electrónico no es válido.');
  if (!/^\d{7,10}$/.test(d.telefono))
    errores.push('El teléfono debe tener entre 7 y 10 dígitos.');
  if (!d.ciudad) errores.push('La ciudad es obligatoria.');
  if (!d.provincia) errores.push('La provincia es obligatoria.');
  return errores;
}

function mostrarErroresFormulario(mensajes) {
  el.formErrores.replaceChildren(
    ...mensajes.map((m) => crearElemento('li', m)),
  );
  el.formErrores.hidden = false;
  el.formErrores.scrollIntoView({ block: 'nearest' });
}

function ocultarErroresFormulario() {
  el.formErrores.replaceChildren();
  el.formErrores.hidden = true;
}

async function guardar(evento) {
  evento.preventDefault();
  ocultarErroresFormulario();

  const datos = leerFormulario();
  const errores = validar(datos);
  if (errores.length > 0) {
    mostrarErroresFormulario(errores);
    return;
  }

  el.btnGuardar.disabled = true;
  try {
    if (idEnEdicion === null) {
      await crear(datos);
      mostrarAviso('Cliente creado correctamente.');
    } else {
      await actualizar(idEnEdicion, datos);
      mostrarAviso('Cliente actualizado correctamente.');
    }
    el.dlgForm.close();
    await cargar();
  } catch (error) {
    mostrarErroresFormulario(mensajesDe(error));
  } finally {
    el.btnGuardar.disabled = false;
  }
}

/* detalle */

function abrirDetalle(cliente) {
  clienteEnDetalle = cliente;
  const campos = [
    ['Id', cliente.id],
    ['Cédula', cliente.cedula],
    ['Nombres', cliente.nombres],
    ['Apellidos', cliente.apellidos],
    ['Fecha de nacimiento', formatearFecha(cliente.fechaNacimiento)],
    ['Género', GENEROS[cliente.genero] ?? cliente.genero],
    ['Correo electrónico', cliente.email],
    ['Teléfono', cliente.telefono],
    ['Dirección', cliente.direccion || '—'],
    ['Ciudad', cliente.ciudad],
    ['Provincia', cliente.provincia],
    ['Estado', cliente.activo ? 'Activo' : 'Inactivo'],
    ['Fecha de registro', formatearFechaHora(cliente.fechaRegistro)],
  ];

  el.detalleLista.replaceChildren();
  for (const [etiqueta, valor] of campos) {
    el.detalleLista.append(
      crearElemento('dt', etiqueta),
      crearElemento('dd', String(valor)),
    );
  }
  el.dlgDetalle.showModal();
}

/*eliminar */

function abrirEliminar(cliente) {
  clienteParaEliminar = cliente;
  el.eliminarTexto.textContent = `Se eliminará a ${cliente.nombreCompleto} (cédula ${cliente.cedula}). Esta acción no se puede deshacer.`;
  el.dlgEliminar.showModal();
}

async function confirmarEliminar() {
  if (!clienteParaEliminar) return;
  el.eliminarConfirmar.disabled = true;
  try {
    await eliminar(clienteParaEliminar.id);
    el.dlgEliminar.close();
    mostrarAviso('Cliente eliminado correctamente.');
  } catch (error) {
    el.dlgEliminar.close();
    mostrarAviso(mensajesDe(error).join(' '), 'error');
  } finally {
    el.eliminarConfirmar.disabled = false;
    clienteParaEliminar = null;
  }
  await cargar();
}
/* arranque */

function iniciar() {
  $('#lista-provincias').append(
    ...PROVINCIAS.map((p) =>
      Object.assign(document.createElement('option'), { value: p }),
    ),
  );
  el.form.elements.fechaNacimiento.max = new Date().toLocaleDateString('sv-SE');

  el.btnNuevo.addEventListener('click', () => abrirFormulario());
  el.form.addEventListener('submit', guardar);
  el.btnCancelar.addEventListener('click', () => el.dlgForm.close());

  el.detalleCerrar.addEventListener('click', () => el.dlgDetalle.close());
  el.detalleEditar.addEventListener('click', () =>
    abrirFormulario(clienteEnDetalle),
  );

  el.eliminarCancelar.addEventListener('click', () => el.dlgEliminar.close());
  el.eliminarConfirmar.addEventListener('click', confirmarEliminar);

  el.formBusqueda.addEventListener('submit', (e) => {
    e.preventDefault();
    clearTimeout(temporizadorBusqueda);
    cargar();
  });
  el.busqueda.addEventListener('input', () => {
    clearTimeout(temporizadorBusqueda);
    temporizadorBusqueda = setTimeout(cargar, 300); // espera a que termine de escribir
  });

  cargar();
}

iniciar();
